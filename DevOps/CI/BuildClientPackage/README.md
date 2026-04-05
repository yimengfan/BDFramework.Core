# BuildClientPackage

每个平台一个 Python 入口，统一调用 Unity BatchMode 构建母包。

## 文件说明

- `build_android.py`
- `build_ios.py`
- `build_windows.py`
- `common.py`
- `config/settings.py`

## 必填参数

- `--client-version`

## 示例

```bash
python3 DevOps/CI/BuildClientPackage/build_android.py --client-version 0.1.0
python3 DevOps/CI/BuildClientPackage/build_ios.py --client-version 0.1.0
python3 DevOps/CI/BuildClientPackage/build_windows.py --client-version 0.1.0
```

## Unity 路径

默认从 `config/settings.py` 读取，也可以通过环境变量覆盖：

```bash
export UNITY_PATH="/Applications/Unity/Hub/Editor/2021.3.58f1/Unity.app/Contents/MacOS/Unity"
```

