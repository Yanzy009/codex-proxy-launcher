import Foundation

@main
struct ProxyConfigurationTests {
    static func main() {
        assert(ProxyConfiguration.parse("http://127.0.0.1:7897")?.absoluteString == "http://127.0.0.1:7897")
        assert(ProxyConfiguration.parse(" http://localhost:8080 ")?.absoluteString == "http://localhost:8080")
        assert(ProxyConfiguration.parse("socks5://localhost:7897") == nil)
        assert(ProxyConfiguration.parse("http://localhost") == nil)
        assert(ProxyConfiguration.parse("http://localhost:70000") == nil)
        assert(ProxyConfiguration.parse("http://user:pass@localhost:7897") == nil)
        assert(ProxyConfiguration.parse("http://localhost:7897/path") == nil)
        let env = ProxyConfiguration.environment(proxyURL: "http://localhost:8080", base: ["ALL_PROXY": "bad", "http_proxy": "bad", "PATH": "/bin"])
        assert(env["HTTP_PROXY"] == "http://localhost:8080")
        assert(env["HTTPS_PROXY"] == "http://localhost:8080")
        assert(env["NO_PROXY"] == "127.0.0.1,localhost,::1")
        assert(env["ALL_PROXY"] == nil && env["http_proxy"] == nil)
        assert(env["PATH"] == "/bin")
        print("proxy configuration tests: OK")
    }
}
