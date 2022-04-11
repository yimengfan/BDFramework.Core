# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/) and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.7.2] - 2020-06-25
### Fixed
- Fixed issue that documentation images are shown in wrong order.

## [1.7.1] - 2020-06-25
### Changed
- Removed dependency with AssetBundleBrowser and Addressables.
- Relaxed minimum version required with Addressables from 1.7.5 to 1.6.0.

### Fixed
- Fixed issue that AddressableBuilder cannot select BuilderScript when there is only one.
- Fixed issue that documentation images are shown in wrong order.


## [1.7.0] - 2020-04-03
### Changed
- Added FileOperation node.
- Added ImportUnityPackage node.
- Added ExportAsUnityPackage node.
- Added AddressableBuilder node.
- Suppress hidden graphs from Default AssetBundle Graph in Project Setting.
- Upgraded Addressables version (1.2.3 -> 1.7.5)

### Fixed
- Fixed issue that graph is unable to execute when a broken event is recorded. (#142)
- Fixed Extract Shared Assets does not correctly find shared assets among variants.

## [1.6.0] - 2019-09-14
### Changed
- Added Animation Import Overwrite Options to let user decide or skip overwriting AnimationClip Settings and Human Descriptions. (#104)
- Dependent package version update: Addressables 1.1.7->1.2.3, Asset Bundle Browser 1.6.0-> 1.7.0

### Fixed
- Fixed EditorTest compile errors (#106)
- Fixed Issue Last Imported Items produces exception (#100)

## [1.5.0] - 2018-07-31
*This is the first version of AssetGraph in the package form.*

### Changed
- Software License now changed to Unity Companion License from MIT License.
- API namespace changed from UnityEngine.AssetGraph to Unity.AssetGraph.
- Default AssetBundleBuildMap path changed. It can be configured from the Project Settings window per project.
- Stopped to try importing old data automatically. Instead, the menu for importing Old version (v1.1) data is now always visible from AssetGraph Window.
- Added 2018.3 support.
- Added 2019.2 support.

### Fixed
- Fixed issue that error icon not displayed properly on node when node has an error. (2018.1)

