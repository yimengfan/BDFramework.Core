#include "il2cpp-config.h"
#include "icalls/mscorlib/System.Diagnostics/StackFrame.h"
#include "vm/StackTrace.h"
#include "gc/GarbageCollector.h"

using namespace mono::vm;

namespace mono
{
namespace icalls
{
namespace mscorlib
{
namespace System
{
namespace Diagnostics
{
    static bool IsCalledFromSystemDiagnosticsStackTrace(const StackFrames& stack)
    {
        for (StackFrames::const_iterator frame = stack.begin(); frame != stack.end(); ++frame)
            if (strcmp(mono_class_get_namespace(mono_unity_method_get_class(frame->method)), "System.Diagnostics") == 0 && strcmp(mono_class_get_name(mono_unity_method_get_class(frame->method)), "StackTrace") == 0)
                return true;

        return false;
    }

    static bool FrameNeedsSkipped(const MonoStackFrameInfo& frame)
    {
        return strcmp(mono_class_get_namespace(mono_unity_method_get_class(frame.method)), "System.Diagnostics") == 0 &&
            (strcmp(mono_class_get_name(mono_unity_method_get_class(frame.method)), "StackFrame") == 0 || strcmp(mono_class_get_name(mono_unity_method_get_class(frame.method)), "StackTrace") == 0);
    }

    bool StackFrame::get_frame_info(
        int32_t skip,
        bool needFileInfo,
        MonoReflectionMethod ** method,
        int32_t* iloffset,
        int32_t* native_offset,
        MonoString** file,
        int32_t* line,
        int32_t* column)
    {
        const int kSkippedFramesFromMSCorlibStackFrameMethods = 2;
        const int kSkippedFramesFromMSCorlibStackTraceMethods = 2;

        const StackFrames& stack = *StackTrace::GetStackFrames();

        // Always ignore the skipped frames from System.Diagnostics.StackFrame, as we know we are always called from it.
        // These frames might be inlined or optimized away by the C++ compiler, so we will inspect the actual stack
        // frames later to see if we need to add skipped frames back in for System.Diagnostics.StackFrame.
        skip -= kSkippedFramesFromMSCorlibStackFrameMethods;


        // Sometimes ignore the skipped frames from System.Diagnostics.StackTrace, as we may or may not be called from it.
        // These frames might be inlined or optimized away by the C++ compiler, so we will inspect the actual stack
        // frames later to see if we need to add skipped frames back in for System.Diagnostics.StackTrace.
        if (IsCalledFromSystemDiagnosticsStackTrace(stack))
            skip -= kSkippedFramesFromMSCorlibStackTraceMethods;

        // Now look in the actual stack trace to see if anything we ignored above is really there. We don't know what the C++
        // compile will do with these frames, so we need to inspect each frame and check.
        for (StackFrames::const_iterator frame = stack.begin(); frame != stack.end(); ++frame)
        {
            if (FrameNeedsSkipped(*frame))
                skip++;
        }

        // Finally, find the location in the stack we actually want to use by offsetting from the end of the stack the number of
        // frames we skipped, and offsetting it by one more to account for this icall itself.
        int64_t index = stack.size() - skip - 1;
        if (static_cast<uint64_t>(index) >= stack.size() || index < 0)
            return false;

        const MonoStackFrameInfo& info = stack[static_cast<size_t>(index)];

        *method = mono_unity_method_get_object(const_cast<MonoMethod*>(info.method));
        il2cpp::gc::GarbageCollector::SetWriteBarrier((void**)method);

        return true;
    }
} /* namespace Diagnostics */
} /* namespace System */
} /* namespace mscorlib */
} /* namespace icalls */
} /* namespace mono */
