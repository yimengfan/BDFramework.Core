#pragma once

#include "il2cpp-config.h"
#include <stdint.h>
#include <vector>
#include "mono-api.h"

namespace mono
{
namespace vm
{
    typedef std::vector<MonoStackFrameInfo> StackFrames;

    class StackTrace
    {
    public:
        static void InitializeStackTracesForCurrentThread();
        static void CleanupStackTracesForCurrentThread();

        // Current thread functions
        static const StackFrames* GetStackFrames();
        static bool GetStackFrameAt(int32_t depth, MonoStackFrameInfo& frame);
        static void WalkFrameStack(MonoInternalStackWalk callback, MonoContext* context, void *user_data);

        inline static size_t GetStackDepth() { return GetStackFrames()->size(); }
        inline static bool GetTopStackFrame(MonoStackFrameInfo& frame) { return GetStackFrameAt(0, frame); }

        static void PushFrame(MonoStackFrameInfo& frame);
        static void PopFrame();

        // Remote thread functions
        static bool GetThreadStackFrameAt(MonoThread* thread, int32_t depth, MonoStackFrameInfo& frame);
        static void WalkThreadFrameStack(MonoThread* thread, MonoInternalStackWalk callback, MonoContext* context, void* user_data);
        static int32_t GetThreadStackDepth(MonoThread* thread);
        static bool GetThreadTopStackFrame(MonoThread* thread, MonoStackFrameInfo& frame);
    };
}
}
