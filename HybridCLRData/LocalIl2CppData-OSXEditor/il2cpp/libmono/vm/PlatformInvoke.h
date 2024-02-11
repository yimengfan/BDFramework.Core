#pragma once

#include "il2cpp-config.h"
#include "il2cpp-object-internals.h"
#include "mono-api.h"
#include "utils/StringView.h"

#include <vector>
#include <string>

namespace mono
{
namespace vm
{
    class PlatformInvoke
    {
    public:
        static char* MarshalCSharpStringToCppString(MonoString* managedString);
        static void MarshalCSharpStringToCppStringFixed(MonoString* managedString, char* buffer, int numberOfCharacters);
        static mono_unichar2* MarshalCSharpStringToCppWString(MonoString* managedString);
        static void MarshalCSharpStringToCppWStringFixed(MonoString* managedString, mono_unichar2* buffer, int numberOfCharacters);

        static MonoString* MarshalCppStringToCSharpStringResult(const char* value);
        static MonoString* MarshalCppWStringToCSharpStringResult(const mono_unichar2* value);

        static char* MarshalEmptyStringBuilder(MonoStringBuilder* stringBuilder);
        static mono_unichar2* MarshalEmptyWStringBuilder(MonoStringBuilder* stringBuilder);

        static char* MarshalStringBuilder(MonoStringBuilder* stringBuilder);
        static mono_unichar2* MarshalWStringBuilder(MonoStringBuilder* stringBuilder);

        static void MarshalStringBuilderResult(MonoStringBuilder* stringBuilder, char* buffer);
        static void MarshalWStringBuilderResult(MonoStringBuilder* stringBuilder, mono_unichar2* buffer);

        static intptr_t MarshalDelegate(MonoDelegate* d);
        static Il2CppDelegate* MarshalFunctionPointerToDelegate(void* functionPtr, MonoClass* delegateType);

        template<typename T>
        static T* MarshalAllocateStringBuffer(size_t numberOfCharacters)
        {
            MonoError unused;
            return (T*)mono_marshal_alloc((unsigned long)numberOfCharacters * sizeof(T), &unused);
        }

    private:
        static char* MarshalEmptyStringBuilder(MonoStringBuilder* stringBuilder, size_t& stringLength, std::vector<std::string>& utf8Chunks, std::vector<MonoStringBuilder*>& builders);
        static mono_unichar2* MarshalEmptyWStringBuilder(MonoStringBuilder* stringBuilder, size_t& stringLength);
    };
} // namespace vm
} // namespace mono
