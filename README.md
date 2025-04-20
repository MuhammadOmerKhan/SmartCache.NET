# SmartCache.NET 🚀

A high-performance, thread-safe, pluggable in-memory caching executor for .NET developers. Supports optional async refresh, TTL-based expiry, and intelligent cache invalidation.

## 🌟 Why SmartCache.NET?

SmartCache.NET helps you reduce redundant computation and improve application responsiveness with:

- ⚡ **Thread-safe caching**
- 🔄 **Async refresh support**
- 🧠 **Smart locking & deduplication**
- ⏱️ **Configurable TTL**
- 💡 Minimal setup — plug and play!

---

## 🔧 How It Works

Just wrap your logic with `Executor.Instance.GetDataAsync` or `GetData` and get optimized performance out of the box!

```csharp
var result = await Executor.Instance.GetDataAsync<string>(() => {
    return ExpensiveComputation();
}, parameters, cacheDuration: 30, asyncRefreshAfterSecs: 120);
```

---
## 📦 Installation
```csharp
dotnet add package SmartCacheNET
```
Or Clone:
```csharp
git clone https://github.com/MuhammadOmerKhan/SmartCache.NET.git
```

---
## 🚀 Benchmark Sample
The Program.cs demonstrates 10 concurrent calls using a shared cache key:
```csharp
[Total CodeBlock Executions]: 1
[Total Time]: 505ms
```
Even with 10 parallel threads — only 1 execution happened. That’s the power of SmartCache.NET.

## 📁 Folder Structure
```csharp
SmartCache.NET
  /Executor.cs  -> Core logic
  /Program.cs   -> Usage demo with benchmark
```

---
📜 License
- MIT License. Do whatever you want — just give credit.
---
## 🤝 Contributing
- Star 🌟 this repo
- Fork 🍴 and submit a PR
- Raise issues or improvements
---
## 🔗 Follow Me  
[![GitHub](https://img.shields.io/badge/GitHub-000?style=for-the-badge&logo=github&logoColor=white)](https://github.com/MuhammadOmerKhan)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-0A66C2?style=for-the-badge&logo=linkedin&logoColor=white)](https://linkedin.com/in/muhammadomerkhan)
[![X](https://img.shields.io/badge/X-1DA1F2?style=for-the-badge&logo=twitter&logoColor=white)](https://twitter.com/omerkhanpk)

---

![MIT License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET 8.0+](https://img.shields.io/badge/.NET-8.0+-purple)
![Made with 💙](https://img.shields.io/badge/Made%20with-.NET-blue)
