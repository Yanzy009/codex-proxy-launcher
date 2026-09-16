import AppKit
import Foundation

@main
struct IconGenerator {
    static func main() throws {
        guard CommandLine.arguments.count == 2 else { throw NSError(domain: "IconGenerator", code: 1) }
        let directory = URL(fileURLWithPath: CommandLine.arguments[1], isDirectory: true)
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        let variants: [(String, Int)] = [
            ("icon_16x16.png", 16), ("icon_16x16@2x.png", 32),
            ("icon_32x32.png", 32), ("icon_32x32@2x.png", 64),
            ("icon_128x128.png", 128), ("icon_128x128@2x.png", 256),
            ("icon_256x256.png", 256), ("icon_256x256@2x.png", 512),
            ("icon_512x512.png", 512), ("icon_512x512@2x.png", 1024)
        ]
        for (name, size) in variants {
            let image = NSImage(size: NSSize(width: size, height: size))
            image.lockFocus()
            let unit = CGFloat(size)
            NSColor(calibratedRed: 0.12, green: 0.16, blue: 0.31, alpha: 1).setFill()
            NSBezierPath(roundedRect: NSRect(x: 0, y: 0, width: unit, height: unit), xRadius: unit * 0.23, yRadius: unit * 0.23).fill()
            NSColor(calibratedRed: 0.25, green: 0.85, blue: 0.78, alpha: 1).setStroke()
            let route = NSBezierPath()
            route.lineWidth = max(2, unit * 0.09)
            route.lineCapStyle = .round
            route.move(to: NSPoint(x: unit * 0.22, y: unit * 0.30))
            route.line(to: NSPoint(x: unit * 0.43, y: unit * 0.52))
            route.line(to: NSPoint(x: unit * 0.62, y: unit * 0.45))
            route.stroke()
            NSColor.white.setFill()
            NSBezierPath(ovalIn: NSRect(x: unit * 0.58, y: unit * 0.57, width: unit * 0.19, height: unit * 0.19)).fill()
            image.unlockFocus()
            guard let tiff = image.tiffRepresentation,
                  let bitmap = NSBitmapImageRep(data: tiff),
                  let png = bitmap.representation(using: .png, properties: [:]) else {
                throw NSError(domain: "IconGenerator", code: 2)
            }
            try png.write(to: directory.appendingPathComponent(name))
        }
    }
}
