import Foundation
import AVFoundation
import ImageIO
let source=URL(fileURLWithPath:CommandLine.arguments[1])
let output=URL(fileURLWithPath:CommandLine.arguments[2],isDirectory:true)
let asset=AVURLAsset(url:source)
let videos=asset.tracks(withMediaType:.video)
precondition(videos.count==1 && asset.tracks(withMediaType:.audio).isEmpty)
let track=videos[0]
precondition(track.naturalSize.width==960 && track.naturalSize.height==720)
precondition(abs(asset.duration.seconds-256.0/30)<0.001 && abs(track.nominalFrameRate-30)<0.01)
let generator=AVAssetImageGenerator(asset:asset)
generator.requestedTimeToleranceBefore = .zero
generator.requestedTimeToleranceAfter = .zero
for frame in [0,15,127,128,143,255] {
 var actual=CMTime.zero
 let image=try generator.copyCGImage(at:CMTime(value:Int64(frame),timescale:30),actualTime:&actual)
 precondition(abs(actual.seconds-Double(frame)/30)<0.0001)
 let path=output.appendingPathComponent(String(format:"decoded-%03d.png",frame))
 let destination=CGImageDestinationCreateWithURL(path as CFURL,"public.png" as CFString,1,nil)!
 CGImageDestinationAddImage(destination,image,nil)
 precondition(CGImageDestinationFinalize(destination))
}
let result:[String:Any] = ["decoder":"macOS AVFoundation","framesDecoded":[0,15,127,128,143,255],"fps":track.nominalFrameRate,"durationSeconds":asset.duration.seconds,"width":track.naturalSize.width,"height":track.naturalSize.height,"audioTracks":0]
let data=try JSONSerialization.data(withJSONObject:result,options:[.prettyPrinted,.sortedKeys])
try data.write(to:output.appendingPathComponent("video-verification.json"))
print(String(data:data,encoding:.utf8)!)
