from __future__ import annotations

 # 说明：
# 1. 这里放“配置”，不放执行流程。
# 2. 每个平台的 build_xxx.py 才是 CI 的执行主体。
# 3. 这里仅提供 Unity BatchMode 相关的候选路径、方法名、日志目录等公共常量。

SETTINGS = {
    # Unity 工程相关配置。
    "project": {
        # 默认工程目录。
        # None 表示从脚本目录自动推导：BuildClientPackage/../../.. => Unity 工程根目录。
        "default_dir": None,
        # Unity 工程目录校验所需的标记目录。
        # 只要这些关键目录都存在，就认为这是一个合法 Unity 工程目录。
        "markers": ["Assets", "Packages", "ProjectSettings"],
    },
    # Unity 版本与可执行文件查找规则。
    "unity": {
        # 默认 Unity 版本。
        # 如果调用时没有显式传入 --unity-version，则使用这个版本。
        "default_version": "2021.3.58f1",
        # 当前脚本允许选择的 Unity 版本列表。
        # 如果要新增版本，只需要在这里补一个版本号。
        "supported_versions": [
            "2021.3.58f1",
            "2022.3.74f1",
        ],
        # Unity 路径使用 key-value 形式配置，方便按名称索引和维护。
        # 结构说明：
        # 1. 第一层 key 是宿主机平台：mac / windows / linux
        # 2. 第二层分为 versioned / fallback 两组
        # 3. 第三层是候选路径名称 => 路径值
        #
        # 其中：
        # - versioned: 需要包含 {version} 占位符，运行时会替换成目标 Unity 版本
        # - fallback: 不区分版本的兜底路径
        #
        # 命名建议：
        # - hub: Unity Hub 默认安装位置
        # - ci: 约定好的 CI 安装位置
        # - legacy: 历史机器路径
        #
        # Python 3.7+ 字典保持插入顺序，因此这里的声明顺序就是查找优先级。
        "paths": {
            "mac": {
                "versioned": {
                    "hub": "/Applications/Unity{version}/Unity.app/Contents/MacOS/Unity",
                },
                "fallback": {},
            },
            "windows": {
                "versioned": {
                    "hub": r"C:\Program Files\Unity\Hub\Editor\{version}\Editor\Unity.exe",
                },
                "fallback": {},
            },
            "linux": {
                "versioned": {
                    "ci": "/opt/unity/{version}/Editor/Unity",
                },
                "fallback": {
                    "opt_unity": "/opt/unity/Editor/Unity",
                    "opt_Unity": "/opt/Unity/Editor/Unity",
                    "usr_local_Unity": "/usr/local/Unity/Editor/Unity",
                },
            },
        },
    },
    # 各平台脚本自己的流程限制和 Unity C# 执行入口。
    # 这里只保留执行真正需要的最小字段：allowed_hosts + method。
    "platforms": {
        "android": {
            "allowed_hosts": ["mac", "windows", "linux"],
            "method": "BDFramework.Editor.DevOps.PublishPipeLineCI.BuildClientPackageAndroid",
        },
        "ios": {
            "allowed_hosts": ["mac"],
            "method": "BDFramework.Editor.DevOps.PublishPipeLineCI.BuildClientPackageIOS",
        },
        "windows": {
            "allowed_hosts": ["mac", "windows", "linux"],
            "method": "BDFramework.Editor.DevOps.PublishPipeLineCI.BuildClientPackageWindows",
        },
    },
    # 日志目录名，最终位于 DevOps/CI/BuildClientPackage/logs 下。
    "log_dir_name": "logs",
}

