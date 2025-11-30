# Introduction

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Unity Version](https://img.shields.io/badge/Unity-2019%2B-blue)](https://unity.com/)

Obfuz is an open-source, powerful, user-friendly, and reliable Unity code obfuscation and protection solution that fully meets the needs of commercial game projects.

[Github](https://github.com/focus-creative-games/obfuz) | [Gitee](https://gitee.com/focus-creative-games/obfuz)

---

## Why Choose Obfuz?

- **Powerful Features**: Provides robust obfuscation and code protection capabilities comparable to commercial tools.
- **Deep Unity Integration**: Optimized for Unity workflows, automatically handling all special cases except reflection (e.g., MonoBehaviour names cannot be obfuscated), working well with zero configuration.
- **Quick Integration**: Simply configure which assemblies to obfuscate and integrate code obfuscation in just three minutes.
- **Stable and Reliable**: Comprehensive automated test projects with over 3000 test cases covering almost all common code scenarios.
- **Hot Update Support**: Supports popular code hot update solutions like HybridCLR and xLua.
- **Agile Development**: Quick response to developer needs, rapid bug fixes, and timely follow-up on the latest changes in Unity and Unite Engine.

## Features

- **Polymorphic DLL Files**: Custom structurally randomized DLL file format with different structures on each release, effectively resisting cracking and tampering.
- **Symbol Obfuscation**: Supports rich configuration rules and incremental obfuscation for flexible and efficient code protection.
- **Constant Obfuscation**: Obfuscates constants like `int`, `long`, `float`, `double`, `string`, and arrays to prevent reverse engineering.
- **Variable Memory Encryption**: Encrypts variables in memory to enhance runtime security.
- **Evaluation Stack Obfuscation**: Obfuscates variables in the execution stack to increase reverse engineering difficulty.
- **Expression Obfuscation**: Obfuscates most common operations like add and sub.
- **Call Obfuscation**:打乱 function call structures to increase cracking difficulty.
- **Control Flow Obfuscation**: Control flow flattening to disrupt code execution flow, significantly increasing reverse engineering difficulty.
- **Random Encryption Virtual Machine**: Generates randomized virtual machines to effectively resist decompilation and cracking tools.
- **Static and Dynamic Decryption**: Combines static and dynamic decryption to prevent offline static analysis.
- **Obfuscation Polymorphism**: Generates different obfuscated code by configuring different generation keys and random seeds.
- **Garbage Code Generation**: Supports multiple garbage code generation methods to improve App Store and Google Play review pass rates.
- **Code Watermarking**: Embeds traceable watermarks.
- **Deep Unity Integration**: Seamlessly integrates with Unity workflows, ready to use with simple configuration.
- **Hot Update Support**: Fully compatible with hot update frameworks like HybridCLR and xLua, ensuring smooth dynamic code updates.
- **DOTS Compatibility**: Compatible with all versions of DOTS without configuration.

## Supported Unity Versions and Platforms

- Supports Unity 2019+
- Supports Unite Engine
- Supports all platforms supported by Unity and Unite Engine
- Supports il2cpp and mono backend

## Documentation

- [Documentation](https://www.obfuz.com/)
- [Quick Start](https://www.obfuz.com/docs/beginner/quick-start)
- [Sample Projects](https://github.com/focus-creative-games/obfuz-samples)

## Future Plans

Obfuz is under continuous development, with upcoming features including:

- **Anti-Memory Dump and Anti-Debugging**: Prevent memory dumping and debugging.
- **Code Virtualization**: Convert code to virtualized instructions for the highest level of security.

## License

Obfuz is released under the MIT License, welcome to use, modify, and distribute freely.

## Contact Us

For questions, suggestions, or bug reports, please contact us through:

- Submit Issues on GitHub
- Email the maintainers: `obfuz#code-philosophy.com`
- QQ Group **Obfuz Community**: 1048396510
- Discord Channel: `https://discord.gg/bFXhmrUw8c`
