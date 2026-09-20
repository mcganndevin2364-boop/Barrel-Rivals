// macOS inspection helper. Encodes actual Unity PNG frames without retiming or image substitution.
// swiftc -module-cache-path <writable-cache> Tools/Art/encode_motion.swift -o <encoder>
// <encoder> <capture-directory> <new-output.mp4>
import Foundation
import AVFoundation
import CoreGraphics
import ImageIO

func require(_ valid: Bool, _ message: String) {
    if !valid { fputs(message + "\n", stderr); exit(1) }
}
require(CommandLine.arguments.count == 3, "Usage: encode_motion capture-directory new-output.mp4")
let directory = URL(fileURLWithPath: CommandLine.arguments[1], isDirectory: true)
let output = URL(fileURLWithPath: CommandLine.arguments[2])
require(!FileManager.default.fileExists(atPath: output.path), "Refusing to replace an existing video.")
let metadata = try JSONSerialization.jsonObject(with: Data(contentsOf: directory.appendingPathComponent("capture.json"))) as! [String: Any]
let width = metadata["width"] as! Int, height = metadata["height"] as! Int
let fps = metadata["framesPerSecond"] as! Int, count = metadata["frameCount"] as! Int
require(fps > 0 && count > 0 && width > 0 && height > 0, "Invalid capture dimensions or timebase.")
let writer = try AVAssetWriter(outputURL: output, fileType: .mp4)
let input = AVAssetWriterInput(mediaType: .video, outputSettings: [
    AVVideoCodecKey: AVVideoCodecType.h264, AVVideoWidthKey: width, AVVideoHeightKey: height,
    AVVideoCompressionPropertiesKey: [AVVideoAverageBitRateKey: 6_000_000, AVVideoMaxKeyFrameIntervalKey: fps,
                                     AVVideoProfileLevelKey: AVVideoProfileLevelH264HighAutoLevel]
])
input.expectsMediaDataInRealTime = false
let adapter = AVAssetWriterInputPixelBufferAdaptor(assetWriterInput: input, sourcePixelBufferAttributes: [
    kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32ARGB,
    kCVPixelBufferWidthKey as String: width, kCVPixelBufferHeightKey as String: height,
    kCVPixelBufferCGImageCompatibilityKey as String: true, kCVPixelBufferCGBitmapContextCompatibilityKey as String: true
])
require(writer.canAdd(input), "Encoder cannot add the video track.")
writer.add(input)
require(writer.startWriting(), "Encoder could not start: \(String(describing: writer.error))")
writer.startSession(atSourceTime: .zero)
let colorSpace = CGColorSpaceCreateDeviceRGB()
for index in 0..<count {
    let deadline = Date().addingTimeInterval(30)
    while !input.isReadyForMoreMediaData && writer.status == .writing && Date() < deadline { Thread.sleep(forTimeInterval: 0.005) }
    require(input.isReadyForMoreMediaData && writer.status == .writing, "Encoder stalled: \(String(describing: writer.error))")
    try autoreleasepool {
        let frame = directory.appendingPathComponent(String(format: "frame-%04d.png", index))
        guard let source = CGImageSourceCreateWithURL(frame as CFURL, nil), let image = CGImageSourceCreateImageAtIndex(source, 0, nil) else {
            throw NSError(domain: "MotionCapture", code: 1, userInfo: [NSLocalizedDescriptionKey: "Missing/invalid frame \(index)"])
        }
        require(image.width == width && image.height == height, "Frame size changed.")
        var optionalBuffer: CVPixelBuffer?
        require(CVPixelBufferPoolCreatePixelBuffer(nil, adapter.pixelBufferPool!, &optionalBuffer) == kCVReturnSuccess, "Could not allocate frame buffer.")
        let buffer = optionalBuffer!
        CVPixelBufferLockBaseAddress(buffer, [])
        guard let context = CGContext(data: CVPixelBufferGetBaseAddress(buffer), width: width, height: height,
            bitsPerComponent: 8, bytesPerRow: CVPixelBufferGetBytesPerRow(buffer), space: colorSpace,
            bitmapInfo: CGImageAlphaInfo.noneSkipFirst.rawValue) else { fatalError("Could not create frame context.") }
        context.draw(image, in: CGRect(x: 0, y: 0, width: width, height: height))
        CVPixelBufferUnlockBaseAddress(buffer, [])
        require(adapter.append(buffer, withPresentationTime: CMTime(value: Int64(index), timescale: Int32(fps))), "Failed to encode frame \(index): \(String(describing: writer.error))")
    }
}
input.markAsFinished()
let completed = DispatchSemaphore(value: 0)
writer.finishWriting { completed.signal() }
require(completed.wait(timeout: .now() + 60) == .success && writer.status == .completed, "Could not finish video: \(String(describing: writer.error))")
let asset = AVURLAsset(url: output)
require(asset.tracks(withMediaType: .video).count == 1, "Encoded output has no video track.")
let duration = CMTimeGetSeconds(asset.duration)
require(abs(duration - Double(count) / Double(fps)) < 0.05, "Encoded duration differs from captured frame timebase.")
print("Encoded \(count) actual Unity frames at \(fps) fps, \(width)x\(height), \(duration) seconds.")
