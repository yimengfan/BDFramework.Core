#include "il2cpp-config.h"
#include "StackTrace.h"
#include <cassert>

#if IL2CPP_DEBUGGER_ENABLED
#include "il2cpp-debugger.h"
#endif // IL2CPP_DEBUGGER_ENABLED

#if IL2CPP_ENABLE_STACKTRACES
#ifdef IL2CPP_ENABLE_NATIVE_STACKTRACES
#include "os/Event.h"
#include "os/StackTrace.h"
#include "os/Thread.h"
#include "vm-utils/NativeSymbol.h"
#endif // IL2CPP_ENABLE_NATIVE_STACKTRACES
#include "os/ThreadLocalValue.h"
#endif // IL2CPP_ENABLE_STACKTRACES

namespace mono
{
namespace vm
{
#if IL2CPP_ENABLE_STACKTRACES

    class MethodStack
    {
    protected:
        il2cpp::os::ThreadLocalValue s_StackFrames;

        inline StackFrames* GetStackFramesRaw()
        {
            StackFrames* stackFrames;

            il2cpp::os::ErrorCode result = s_StackFrames.GetValue(reinterpret_cast<void**>(&stackFrames));
            assert(result == il2cpp::os::kErrorCodeSuccess);

            return stackFrames;
        }

    public:
        inline void InitializeForCurrentThread()
        {
            if (GetStackFramesRaw() != NULL)
                return;

            StackFrames* stackFrames = new StackFrames();
            stackFrames->reserve(64);

            il2cpp::os::ErrorCode result = s_StackFrames.SetValue(stackFrames);
            assert(result == il2cpp::os::kErrorCodeSuccess);
        }

        inline void CleanupForCurrentThread()
        {
            StackFrames* frames = GetStackFramesRaw();

            if (frames == NULL)
                return;

            delete frames;

            il2cpp::os::ErrorCode result = s_StackFrames.SetValue(NULL);
            assert(result == il2cpp::os::kErrorCodeSuccess);
        }
    };

#if IL2CPP_ENABLE_STACKTRACE_SENTRIES

    class StacktraceSentryMethodStack : public MethodStack
    {
    public:
        inline const StackFrames* GetStackFrames()
        {
            return GetStackFramesRaw();
        }

        inline bool GetStackFrameAt(int32_t depth, MonoStackFrameInfo& frame)
        {
            const StackFrames& frames = *GetStackFramesRaw();

            if (static_cast<int>(frames.size()) + depth < 1)
                return false;

            frame = frames[frames.size() - 1 + depth];
            return true;
        }

        inline void PushFrame(MonoStackFrameInfo& frame)
        {
            GetStackFramesRaw()->push_back(frame);

#if IL2CPP_DEBUGGER_ENABLED
            il2cpp_debugger_method_entry(frame);
#endif
        }

        inline void PopFrame()
        {
            StackFrames* stackFrames = GetStackFramesRaw();

#if IL2CPP_DEBUGGER_ENABLED
            MonoStackFrameInfo frame = stackFrames->back();
#endif

            stackFrames->pop_back();

#if IL2CPP_DEBUGGER_ENABLED
            il2cpp_debugger_method_exit(frame);
#endif
        }
    };

#endif // IL2CPP_ENABLE_STACKTRACE_SENTRIES

#if IL2CPP_ENABLE_NATIVE_STACKTRACES

    class NativeMethodStack : public MethodStack
    {
        static bool GetStackFramesCallback(Il2CppMethodPointer frame, void* context)
        {
            MonoMethod* method = const_cast<MonoMethod*>(il2cpp::utils::NativeSymbol::GetMethodFromNativeSymbol(frame));
            StackFrames* stackFrames = static_cast<StackFrames*>(context);

            if (method != NULL)
            {
                MonoStackFrameInfo frameInfo;
                memset(&frameInfo, 0, sizeof(frameInfo));
                frameInfo.method = method;
                frameInfo.actual_method = method;
                frameInfo.type = FRAME_TYPE_MANAGED;
                frameInfo.managed = 1;
                frameInfo.il_offset = 0;
                frameInfo.native_offset = 0;
                frameInfo.ji = (MonoJitInfo*)-1;
                stackFrames->push_back(frameInfo);
            }

            return true;
        }

        struct GetStackFrameAtContext
        {
            int32_t currentDepth;
            const MonoMethod* method;
        };

        static bool GetStackFrameAtCallback(Il2CppMethodPointer frame, void* context)
        {
            const MonoMethod* method = il2cpp::utils::NativeSymbol::GetMethodFromNativeSymbol(frame);
            GetStackFrameAtContext* ctx = static_cast<GetStackFrameAtContext*>(context);

            if (method != NULL)
            {
                if (ctx->currentDepth == 0)
                {
                    ctx->method = method;
                    return false;
                }

                ctx->currentDepth++;
            }

            return true;
        }

    public:
        inline const StackFrames* GetStackFrames()
        {
            StackFrames* stackFrames = GetStackFramesRaw();
            stackFrames->clear();

            il2cpp::os::StackTrace::WalkStack(&NativeMethodStack::GetStackFramesCallback, stackFrames, il2cpp::os::StackTrace::kFirstCalledToLastCalled);

            return stackFrames;
        }

        inline bool GetStackFrameAt(int32_t depth, MonoStackFrameInfo& frame)
        {
            GetStackFrameAtContext context = { depth, NULL };

            il2cpp::os::StackTrace::WalkStack(&NativeMethodStack::GetStackFrameAtCallback, &context, il2cpp::os::StackTrace::kLastCalledToFirstCalled);

            if (context.method != NULL)
            {
                MonoMethod* method = const_cast<MonoMethod*>(context.method);
                frame.method = method;
                frame.actual_method = method;
                frame.type = FRAME_TYPE_MANAGED;
                frame.managed = 1;
                frame.il_offset = 0;
                frame.native_offset = 0;
                frame.ji = (MonoJitInfo*)-1;
                return true;
            }

            return false;
        }

        inline void PushFrame(MonoStackFrameInfo& frame)
        {
        }

        inline void PopFrame()
        {
        }
    };

#endif // IL2CPP_ENABLE_NATIVE_STACKTRACES

#else

    static StackFrames s_EmptyStack;

    class NoOpMethodStack
    {
    public:
        inline void InitializeForCurrentThread()
        {
        }

        inline void CleanupForCurrentThread()
        {
        }

        inline const StackFrames* GetStackFrames()
        {
            return &s_EmptyStack;
        }

        inline bool GetStackFrameAt(int32_t depth, MonoStackFrameInfo& frame)
        {
            return false;
        }

        inline void PushFrame(MonoStackFrameInfo& frame)
        {
        }

        inline void PopFrame()
        {
        }
    };

#endif // IL2CPP_ENABLE_STACKTRACES

#if IL2CPP_ENABLE_STACKTRACES

#if IL2CPP_ENABLE_STACKTRACE_SENTRIES

    StacktraceSentryMethodStack s_MethodStack;

#elif IL2CPP_ENABLE_NATIVE_STACKTRACES

    NativeMethodStack s_MethodStack;

#endif

#else

    NoOpMethodStack s_MethodStack;

#endif // IL2CPP_ENABLE_STACKTRACES

// Current thread functions

    void StackTrace::InitializeStackTracesForCurrentThread()
    {
        s_MethodStack.InitializeForCurrentThread();
    }

    void StackTrace::CleanupStackTracesForCurrentThread()
    {
        s_MethodStack.CleanupForCurrentThread();
    }

    const StackFrames* StackTrace::GetStackFrames()
    {
        return s_MethodStack.GetStackFrames();
    }

    bool StackTrace::GetStackFrameAt(int32_t depth, MonoStackFrameInfo& frame)
    {
        assert(depth <= 0 && "Frame depth must be 0 or less");
        return s_MethodStack.GetStackFrameAt(depth, frame);
    }

    void StackTrace::WalkFrameStack(MonoInternalStackWalk callback, MonoContext* context, void *user_data)
    {
        const StackFrames& frames = *GetStackFrames();

        for (StackFrames::const_reverse_iterator it = frames.rbegin(); it != frames.rend(); ++it)
        {
            if (callback(const_cast<MonoStackFrameInfo*>(&*it), context, user_data))
                break;
        }
    }

    void StackTrace::PushFrame(MonoStackFrameInfo& frame)
    {
        s_MethodStack.PushFrame(frame);
    }

    void StackTrace::PopFrame()
    {
        s_MethodStack.PopFrame();
    }

// Remote thread functions

    struct GetThreadFrameAtContext
    {
        //il2cpp::os::Event apcDoneEvent;
        int32_t depth;
        MonoStackFrameInfo* frame;
        bool hasResult;
    };

    struct WalkThreadFrameStackContext
    {
        //il2cpp::os::Event apcDoneEvent;
        MonoInternalStackWalk callback;
        MonoContext* context;
        void *user_data;
    };

    struct GetThreadStackDepthContext
    {
        //il2cpp::os::Event apcDoneEvent;
        int32_t stackDepth;
    };

    struct GetThreadTopFrameContext
    {
        //il2cpp::os::Event apcDoneEvent;
        MonoStackFrameInfo* frame;
        bool hasResult;
    };

    static void STDCALL GetThreadFrameAtCallback(void* context)
    {
        GetThreadFrameAtContext* ctx = static_cast<GetThreadFrameAtContext*>(context);

        ctx->hasResult = StackTrace::GetStackFrameAt(ctx->depth, *ctx->frame);
        //ctx->apcDoneEvent.Set();
    }

    bool StackTrace::GetThreadStackFrameAt(MonoThread* thread, int32_t depth, MonoStackFrameInfo& frame)
    {
#if IL2CPP_ENABLE_STACKTRACES
        //GetThreadFrameAtContext apcContext;

        //apcContext.depth = depth;
        //apcContext.frame = &frame;

        //thread->GetInternalThread()->handle->QueueUserAPC(GetThreadFrameAtCallback, &apcContext);
        //apcContext.apcDoneEvent.Wait();

        //return apcContext.hasResult;
        return false;
#else
        return false;
#endif
    }

    static void STDCALL WalkThreadFrameStackCallback(void* context)
    {
        WalkThreadFrameStackContext* ctx = static_cast<WalkThreadFrameStackContext*>(context);

        StackTrace::WalkFrameStack(ctx->callback, ctx->context, ctx->user_data);
        //ctx->apcDoneEvent.Set();
    }

    void StackTrace::WalkThreadFrameStack(MonoThread* thread, MonoInternalStackWalk callback, MonoContext *context, void *user_data)
    {
#if IL2CPP_ENABLE_STACKTRACES
        WalkThreadFrameStackContext apcContext;

        apcContext.callback = callback;
        apcContext.context = context;
        apcContext.user_data = user_data;

        //thread->GetInternalThread()->handle->QueueUserAPC(WalkThreadFrameStackCallback, &apcContext);
        //apcContext.apcDoneEvent.Wait();
#endif
    }

    static void STDCALL GetThreadStackDepthCallback(void* context)
    {
        GetThreadStackDepthContext* ctx = static_cast<GetThreadStackDepthContext*>(context);

        ctx->stackDepth = static_cast<int32_t>(StackTrace::GetStackDepth());
        //ctx->apcDoneEvent.Set();
    }

    int32_t StackTrace::GetThreadStackDepth(MonoThread* thread)
    {
#if IL2CPP_ENABLE_STACKTRACES
        //GetThreadStackDepthContext apcContext;

        //thread->GetInternalThread()->handle->QueueUserAPC(GetThreadStackDepthCallback, &apcContext);
        //apcContext.apcDoneEvent.Wait();

        //return apcContext.stackDepth;
        return 0;
#else
        return 0;
#endif
    }

    static void STDCALL GetThreadTopFrameCallback(void* context)
    {
        GetThreadTopFrameContext* ctx = static_cast<GetThreadTopFrameContext*>(context);

        ctx->hasResult = StackTrace::GetTopStackFrame(*ctx->frame);
        //ctx->apcDoneEvent.Set();
    }

    bool StackTrace::GetThreadTopStackFrame(MonoThread* thread, MonoStackFrameInfo& frame)
    {
#if IL2CPP_ENABLE_STACKTRACES
        //GetThreadTopFrameContext apcContext;
        //apcContext.frame = &frame;

        //thread->GetInternalThread()->handle->QueueUserAPC(GetThreadTopFrameCallback, &apcContext);
        //apcContext.apcDoneEvent.Wait();

        //return apcContext.hasResult;
        return false;
#else
        return false;
#endif
    }
}
}
