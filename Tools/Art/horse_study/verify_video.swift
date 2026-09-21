import Foundation
import AVFoundation
import ImageIO

// Verify and decode the rendered study; this measures file timing, not game FPS.
let source = URL(fileURLWithPath: CommandLine.arguments[1])
let output = URL(fileURLWithPath: CommandLine.arguments[2], isDirectory: true)
let specData = try Data(contentsOf: output.appendingPathComponent("video-spec.json"))
let spec = try JSONSerialization.jsonObject(with: specData) as! [String: Any]
let frameCount = spec["frames"] as! Int
let fps = spec["fps"] as! Int
let asset = AVURLAsset(url: source)
let videos = try await asset.loadTracks(withMediaType: .video)
let audio = try await asset.loadTracks(withMediaType: .audio)
precondition(videos.count == 1 && audio.isEmpty)
let track = videos[0]
let size = try await track.load(.naturalSize)
let rate = try await track.load(.nominalFrameRate)
let duration = try await asset.load(.duration)
precondition(size.width == CGFloat(spec["width"] as! Int) && size.height == CGFloat(spec["height"] as! Int))
precondition(abs(duration.seconds - Double(frameCount) / Double(fps)) < 0.001)
precondition(abs(rate - Float(fps)) < 0.01)
let generator = AVAssetImageGenerator(asset: asset)
generator.requestedTimeToleranceBefore = .zero
generator.requestedTimeToleranceAfter = .zero
let views = spec["views"] as! [String]
precondition(!views.isEmpty && frameCount % views.count == 0)
let length = frameCount / views.count
let indices = views.indices.flatMap { [$0 * length, ($0 + 1) * length - 1] }
for index in indices {
    let frame = try await generator.image(at: CMTime(value: Int64(index), timescale: Int32(fps)))
    precondition(abs(frame.actualTime.seconds - Double(index) / Double(fps)) < 0.0001)
    let path = output.appendingPathComponent(String(format: "decoded-%03d.png", index))
    let destination = CGImageDestinationCreateWithURL(path as CFURL, "public.png" as CFString, 1, nil)!
    CGImageDestinationAddImage(destination, frame.image, nil)
    precondition(CGImageDestinationFinalize(destination))
}
let result: [String: Any] = ["decoder": "macOS AVFoundation", "framesDecoded": indices,
    "fps": rate, "durationSeconds": duration.seconds, "width": size.width,
    "height": size.height, "audioTracks": audio.count]
let data = try JSONSerialization.data(withJSONObject: result, options: [.prettyPrinted, .sortedKeys])
try data.write(to: output.appendingPathComponent("video-verification.json"))
print(String(data: data, encoding: .utf8)!)
