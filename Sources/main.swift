import AppKit
import CFNetwork
import Foundation
import SystemConfiguration
import UniformTypeIdentifiers

private let codexBundleID = "com.openai.codex"
private let proxyKey = "proxyURL"
private let codexPathKey = "codexAppPath"
private let lastCodexPIDKey = "lastCodexPID"
private let lastLaunchProxyKey = "lastLaunchProxy"

private func alert(_ title: String, _ message: String, buttons: [String]) -> NSApplication.ModalResponse {
    NSApp.activate(ignoringOtherApps: true)
    let panel = NSAlert()
    panel.messageText = title
    panel.informativeText = message
    buttons.forEach { panel.addButton(withTitle: $0) }
    return panel.runModal()
}

private func systemProxyCandidate() -> String? {
    guard let settings = CFNetworkCopySystemProxySettings()?.takeRetainedValue() as? [String: Any] else { return nil }
    for prefix in ["HTTPS", "HTTP"] {
        guard (settings["\(prefix)Enable"] as? Int) == 1,
              let host = settings["\(prefix)Proxy"] as? String,
              let port = settings["\(prefix)Port"] as? Int,
              port > 0 else { continue }
        return "http://\(host):\(port)"
    }
    return nil
}

private func configureProxy(suggested: String) -> String? {
    var suggestion = suggested
    while true {
        let field = NSTextField(string: suggestion)
        field.placeholderString = "http://127.0.0.1:7897"
        field.frame = NSRect(x: 0, y: 0, width: 360, height: 28)
        let panel = NSAlert()
        panel.messageText = "设置本机 HTTP 代理"
        panel.informativeText = "填写代理软件提供的 HTTP 或混合代理地址。保存后，平时点击启动器即可打开 Codex。"
        panel.accessoryView = field
        panel.addButton(withTitle: "保存")
        panel.addButton(withTitle: "取消")
        NSApp.activate(ignoringOtherApps: true)
        guard panel.runModal() == .alertFirstButtonReturn else { return nil }
        suggestion = field.stringValue
        if let url = ProxyConfiguration.parse(suggestion) {
            UserDefaults.standard.set(url.absoluteString, forKey: proxyKey)
            return url.absoluteString
        }
        _ = alert("代理地址无效", "请填写 http://主机:端口，例如 http://127.0.0.1:7897。", buttons: ["重新填写"])
    }
}

private func proxyIsReachable(_ proxy: String) -> Bool {
    guard let url = ProxyConfiguration.parse(proxy), let host = url.host, let port = url.port else { return false }
    let check = Process()
    check.executableURL = URL(fileURLWithPath: "/usr/bin/nc")
    check.arguments = ["-z", "-G", "2", host, String(port)]
    do {
        try check.run()
        check.waitUntilExit()
        return check.terminationStatus == 0
    } catch { return false }
}

private func executable(for appURL: URL) -> URL? {
    guard let bundle = Bundle(url: appURL), bundle.bundleIdentifier == codexBundleID,
          let executable = bundle.executableURL,
          FileManager.default.isExecutableFile(atPath: executable.path) else { return nil }
    return executable
}

private func discoverCodex() -> URL? {
    var candidates: [URL] = []
    if let saved = UserDefaults.standard.string(forKey: codexPathKey) {
        candidates.append(URL(fileURLWithPath: saved))
    }
    if let located = NSWorkspace.shared.urlForApplication(withBundleIdentifier: codexBundleID) {
        candidates.append(located)
    }
    candidates.append(URL(fileURLWithPath: "/Applications/ChatGPT.app"))
    candidates.append(FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent("Applications/ChatGPT.app"))
    return candidates.first { executable(for: $0) != nil }
}

private func chooseCodex() -> URL? {
    let panel = NSOpenPanel()
    panel.title = "选择官方 Codex 应用"
    panel.message = "选择已安装的 ChatGPT.app / Codex.app。启动器会检查其应用标识。"
    panel.allowedContentTypes = [.applicationBundle]
    panel.canChooseDirectories = false
    panel.canChooseFiles = true
    panel.allowsMultipleSelection = false
    guard panel.runModal() == .OK, let url = panel.url else { return nil }
    guard executable(for: url) != nil else {
        _ = alert("不是 Codex 应用", "所选应用的标识不匹配。请从官方来源安装 Codex 后重试。", buttons: ["好"])
        return nil
    }
    UserDefaults.standard.set(url.path, forKey: codexPathKey)
    return url
}

private func runningCodex() -> NSRunningApplication? {
    NSRunningApplication.runningApplications(withBundleIdentifier: codexBundleID).first
}

private func waitUntil(_ condition: () -> Bool, timeout: TimeInterval) -> Bool {
    let deadline = Date().addingTimeInterval(timeout)
    while Date() < deadline {
        if condition() { return true }
        RunLoop.current.run(until: Date().addingTimeInterval(0.2))
    }
    return condition()
}

private func activateCodex() {
    if waitUntil({ runningCodex() != nil }, timeout: 10) {
        runningCodex()?.activate(options: [.activateAllWindows])
    }
}

private func runSelfTest() -> Int32 {
    let detected = discoverCodex()
    let proxy = UserDefaults.standard.string(forKey: proxyKey) ?? systemProxyCandidate() ?? ""
    print("codex_found=\(detected != nil)")
    print("proxy_valid=\(ProxyConfiguration.parse(proxy) != nil)")
    print("proxy_reachable=\(proxyIsReachable(proxy))")
    print("codex_running=\(runningCodex() != nil)")
    return 0
}

if CommandLine.arguments.contains("--self-test") { exit(runSelfTest()) }

let app = NSApplication.shared
app.setActivationPolicy(.accessory)

let optionPressed = NSEvent.modifierFlags.contains(.option)
var proxy = UserDefaults.standard.string(forKey: proxyKey)
if proxy == nil || optionPressed {
    let suggestion = proxy ?? systemProxyCandidate() ?? "http://127.0.0.1:7897"
    guard let configured = configureProxy(suggested: suggestion) else { exit(0) }
    proxy = configured
}
guard let initialProxy = proxy else { exit(1) }
var selectedProxy = initialProxy

while !proxyIsReachable(selectedProxy) {
    let choice = alert("代理暂时不可连接", "当前代理地址 \(selectedProxy) 无法连接。请确认代理软件已运行，或修改地址。", buttons: ["修改地址", "仍然启动", "取消"])
    if choice == .alertFirstButtonReturn {
        guard let configured = configureProxy(suggested: selectedProxy) else { exit(0) }
        selectedProxy = configured
    } else if choice == .alertSecondButtonReturn { break }
    else { exit(0) }
}

var codexApp = discoverCodex()
if codexApp == nil { codexApp = chooseCodex() }
guard let codexApp, let codexExecutable = executable(for: codexApp) else { exit(1) }

if let existing = runningCodex() {
    let lastPID = UserDefaults.standard.integer(forKey: lastCodexPIDKey)
    let lastProxy = UserDefaults.standard.string(forKey: lastLaunchProxyKey)
    if existing.processIdentifier == lastPID && lastProxy == selectedProxy {
        existing.activate(options: [.activateAllWindows])
        exit(0)
    }
    let choice = alert("Codex 已在运行", "代理设置只会在 Codex 启动时生效。重启前请确认没有正在运行的重要任务。", buttons: ["重启 Codex", "打开现有 Codex", "取消"])
    if choice == .alertFirstButtonReturn {
        _ = existing.terminate()
        guard waitUntil({ runningCodex() == nil }, timeout: 15) else {
            _ = alert("Codex 未能退出", "请手动退出 Codex 后再次打开启动器。", buttons: ["好"])
            exit(1)
        }
    } else if choice == .alertSecondButtonReturn {
        existing.activate(options: [.activateAllWindows])
        exit(0)
    } else { exit(0) }
}

let process = Process()
process.executableURL = codexExecutable
process.environment = ProxyConfiguration.environment(proxyURL: selectedProxy, base: ProcessInfo.processInfo.environment)
do {
    try process.run()
    UserDefaults.standard.set(Int(process.processIdentifier), forKey: lastCodexPIDKey)
    UserDefaults.standard.set(selectedProxy, forKey: lastLaunchProxyKey)
    activateCodex()
} catch {
    _ = alert("Codex 启动失败", error.localizedDescription, buttons: ["好"])
    exit(1)
}
