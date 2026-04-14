/**
 * Talos E2E 公共类型导出。
 */

export { UnityConnector, MsgType, ActionType } from './unity-connector';
export type { TestDescriptor, TestResult, AllTestsComplete, ActionResult, ScreenshotResult, EditorCommandResult } from './unity-connector';
export { DeviceManager } from './device-manager';
export type { Platform, DeviceConfig } from './device-manager';
export {
  UnityEditorOps,
  EditorSceneOps,
  EditorPlayModeOps,
  EditorBridgeOps,
  EditorReflectionOps,
} from './unity-editor-ops';
export type {
  SceneInfo,
  EditorState,
} from './unity-editor-ops';
