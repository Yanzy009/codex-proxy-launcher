import Foundation

enum ProxyConfiguration {
    static func parse(_ raw: String) -> URL? {
        guard let parts = URLComponents(string: raw.trimmingCharacters(in: .whitespacesAndNewlines)),
              parts.scheme?.lowercased() == "http",
              let host = parts.host, !host.isEmpty,
              let port = parts.port, (1...65535).contains(port),
              parts.user == nil, parts.password == nil,
              (parts.path.isEmpty || parts.path == "/"),
              parts.query == nil, parts.fragment == nil else { return nil }
        var normalized = URLComponents()
        normalized.scheme = "http"
        normalized.host = host
        normalized.port = port
        return normalized.url
    }

    static func environment(proxyURL: String, base: [String: String]) -> [String: String] {
        var result = base
        for key in ["ALL_PROXY", "all_proxy", "http_proxy", "https_proxy", "no_proxy"] {
            result.removeValue(forKey: key)
        }
        result["HTTP_PROXY"] = proxyURL
        result["HTTPS_PROXY"] = proxyURL
        result["NO_PROXY"] = "127.0.0.1,localhost,::1"
        return result
    }
}
