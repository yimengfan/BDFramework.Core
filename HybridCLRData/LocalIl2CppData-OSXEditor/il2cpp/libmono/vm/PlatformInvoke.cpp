#include <algorithm>
#include "PlatformInvoke.h"

#include "il2cpp-mono-support.h"
#include "il2cpp-mapping.h"
#include "il2cpp-metadata.h"
#include "utils/StringUtils.h"
#include "vm-utils/NativeDelegateMethodCache.h"
#include "mono-api.h"
#include "MetadataCache.h"

extern const Il2CppCodeRegistration g_CodeRegistration IL2CPP_ATTRIBUTE_WEAK;

namespace mono
{
namespace vm
{
    static mono_unichar2* GetFirstElementAddress(MonoArray *array)
    {
        return reinterpret_cast<mono_unichar2*>(reinterpret_cast<char*>(array) + kIl2CppSizeOfArray);
    }

    char* PlatformInvoke::MarshalCSharpStringToCppString(MonoString* managedString)
    {
        if (managedString == NULL)
            return NULL;

        char *utf8String = mono_string_to_utf8(managedString);

        char* nativeString = MarshalAllocateStringBuffer<char>(mono_string_length(managedString) + 1);
        strcpy(nativeString, utf8String);
        mono_unity_g_free(utf8String);

        return nativeString;
    }

    void PlatformInvoke::MarshalCSharpStringToCppStringFixed(MonoString* managedString, char* buffer, int numberOfCharacters)
    {
        if (managedString == NULL)
        {
            *buffer = '\0';
        }
        else
        {
            char *utf8String = mono_string_to_utf8(managedString);
            strncpy(buffer, utf8String, numberOfCharacters - 1);
            mono_unity_g_free(utf8String);
        }
    }

    mono_unichar2* PlatformInvoke::MarshalCSharpStringToCppWString(MonoString* managedString)
    {
        if (managedString == NULL)
            return NULL;

        int stringLength = mono_string_length(managedString);
        mono_unichar2* nativeString = MarshalAllocateStringBuffer<mono_unichar2>(stringLength + 1);
        for (int32_t i = 0; i < stringLength; ++i)
            nativeString[i] = managedString->chars[i];

        nativeString[managedString->length] = '\0';

        return nativeString;
    }

    void PlatformInvoke::MarshalCSharpStringToCppWStringFixed(MonoString* managedString, mono_unichar2* buffer, int numberOfCharacters)
    {
        if (managedString == NULL)
        {
            *buffer = '\0';
        }
        else
        {
            int32_t stringLength = std::min(mono_string_length(managedString), numberOfCharacters - 1);
            for (int32_t i = 0; i < stringLength; ++i)
                buffer[i] = managedString->chars[i];

            buffer[stringLength] = '\0';
        }
    }

    MonoString* PlatformInvoke::MarshalCppStringToCSharpStringResult(const char* value)
    {
        if (value == NULL)
            return NULL;

        return mono_string_new(g_MonoDomain, value);
    }

    MonoString* PlatformInvoke::MarshalCppWStringToCSharpStringResult(const mono_unichar2* value)
    {
        if (value == NULL)
            return NULL;

        return mono_string_new_utf16(g_MonoDomain, value, (int)il2cpp::utils::StringUtils::StrLen(value));
    }

    char* PlatformInvoke::MarshalEmptyStringBuilder(MonoStringBuilder* stringBuilder, size_t& stringLength, std::vector<std::string>& utf8Chunks, std::vector<MonoStringBuilder*>& builders)
    {
        if (stringBuilder == NULL)
            return NULL;

        stringLength = 0;
        MonoStringBuilder* currentBuilder = stringBuilder;

        while (true)
        {
            if (currentBuilder == NULL)
                break;

            const mono_unichar2 *str = GetFirstElementAddress(currentBuilder->chunkChars);
            std::string utf8String = il2cpp::utils::StringUtils::Utf16ToUtf8((Il2CppChar*)str, (int)currentBuilder->chunkChars->max_length);

            utf8Chunks.push_back(utf8String);
            builders.push_back(currentBuilder);

            size_t lenToCount = std::max((size_t)currentBuilder->chunkChars->max_length, utf8String.size());

            stringLength += lenToCount;

            currentBuilder = currentBuilder->chunkPrevious;
        }

        char* nativeString = MarshalAllocateStringBuffer<char>(stringLength + 1);

        // We need to zero out the memory because the chunkChar array lengh may have been larger than the chunkLength
        // and when this happens we'll have a utf8String that is smaller than the the nativeString we allocated.  When we go to copy the
        // chunk utf8String into the nativeString it won't fill everything and we can end up with w/e junk value was in that memory before
        memset(nativeString, 0, sizeof(char) * (stringLength + 1));

        return nativeString;
    }

    char* PlatformInvoke::MarshalEmptyStringBuilder(MonoStringBuilder* stringBuilder)
    {
        size_t sizeLength;
        std::vector<std::string> utf8Chunks;
        std::vector<MonoStringBuilder*> builders;
        return MarshalEmptyStringBuilder(stringBuilder, sizeLength, utf8Chunks, builders);
    }

    char* PlatformInvoke::MarshalStringBuilder(MonoStringBuilder* stringBuilder)
    {
        if (stringBuilder == NULL)
            return NULL;

        size_t stringLength;
        std::vector<std::string> utf8Chunks;
        std::vector<MonoStringBuilder*> builders;
        char* nativeString = MarshalEmptyStringBuilder(stringBuilder, stringLength, utf8Chunks, builders);

        if (stringLength > 0)
        {
            int offsetAdjustment = 0;
            for (int i = (int)utf8Chunks.size() - 1; i >= 0; i--)
            {
                std::string utf8String = utf8Chunks[i];

                const char* utf8CString = utf8String.c_str();

                memcpy(nativeString + builders[i]->chunkOffset + offsetAdjustment, utf8CString, (int)utf8String.size());

                offsetAdjustment += (int)utf8String.size() - builders[i]->chunkLength;
            }
        }

        return nativeString;
    }

    mono_unichar2* PlatformInvoke::MarshalEmptyWStringBuilder(MonoStringBuilder* stringBuilder, size_t& stringLength)
    {
        if (stringBuilder == NULL)
            return NULL;

        stringLength = 0;
        MonoStringBuilder* currentBuilder = stringBuilder;
        while (true)
        {
            if (currentBuilder == NULL)
                break;

            stringLength += (size_t)currentBuilder->chunkChars->max_length;

            currentBuilder = currentBuilder->chunkPrevious;
        }

        return MarshalAllocateStringBuffer<mono_unichar2>(stringLength + 1);
    }

    mono_unichar2* PlatformInvoke::MarshalEmptyWStringBuilder(MonoStringBuilder* stringBuilder)
    {
        size_t stringLength;
        return MarshalEmptyWStringBuilder(stringBuilder, stringLength);
    }

    mono_unichar2* PlatformInvoke::MarshalWStringBuilder(MonoStringBuilder* stringBuilder)
    {
        if (stringBuilder == NULL)
            return NULL;

        size_t stringLength;
        mono_unichar2* nativeString = MarshalEmptyWStringBuilder(stringBuilder, stringLength);

        if (stringLength > 0)
        {
            MonoStringBuilder* currentBuilder = stringBuilder;
            while (true)
            {
                if (currentBuilder == NULL)
                    break;

                const mono_unichar2 *str = GetFirstElementAddress(currentBuilder->chunkChars);

                memcpy(nativeString + currentBuilder->chunkOffset, str, (int)currentBuilder->chunkChars->max_length * sizeof(mono_unichar2));

                currentBuilder = currentBuilder->chunkPrevious;
            }
        }

        nativeString[stringLength] = '\0';

        return nativeString;
    }

    void PlatformInvoke::MarshalStringBuilderResult(MonoStringBuilder* stringBuilder, char* buffer)
    {
        if (stringBuilder == NULL || buffer == NULL)
            return;

        UTF16String utf16String = il2cpp::utils::StringUtils::Utf8ToUtf16(buffer);

        MONO_OBJECT_SETREF(stringBuilder, chunkChars, MonoArrayNew(mono_unity_defaults_get_char_class(), (int)utf16String.size() + 1));

        for (int i = 0; i < (int)utf16String.size(); i++)
            mono_array_set(stringBuilder->chunkChars, mono_unichar2, i, utf16String[i]);

        mono_array_set(stringBuilder->chunkChars, mono_unichar2, (int)utf16String.size(), '\0');

        stringBuilder->chunkLength = (int)utf16String.size();
        stringBuilder->chunkOffset = 0;
        MONO_OBJECT_SETREF(stringBuilder, chunkPrevious, NULL);
    }

    void PlatformInvoke::MarshalWStringBuilderResult(MonoStringBuilder* stringBuilder, mono_unichar2* buffer)
    {
        if (stringBuilder == NULL || buffer == NULL)
            return;

        int len = (int)il2cpp::utils::StringUtils::StrLen(buffer);

        MONO_OBJECT_SETREF(stringBuilder, chunkChars, MonoArrayNew(mono_unity_defaults_get_char_class(), len + 1));

        for (int i = 0; i < len; i++)
            mono_array_set(stringBuilder->chunkChars, mono_unichar2, i, buffer[i]);

        mono_array_set(stringBuilder->chunkChars, mono_unichar2, len, '\0');

        stringBuilder->chunkLength = len;
        stringBuilder->chunkOffset = 0;
        MONO_OBJECT_SETREF(stringBuilder, chunkPrevious, NULL);
    }

    static int CompareIl2CppTokenIndexMethodTuple(const void* pkey, const void* pelem)
    {
        return (int)(((Il2CppTokenIndexMethodTuple*)pkey)->token - ((Il2CppTokenIndexMethodTuple*)pelem)->token);
    }

    static Il2CppMethodPointer GetReversePInvokeWrapperFromIndex(MonoMethod* method)
    {
        Il2CppCodeGenModule* codeGenModule = InitializeCodeGenHandle(method->klass->image);

        if (codeGenModule->reversePInvokeWrapperCount == 0)
            return NULL;

        Il2CppTokenIndexMethodTuple key;
        memset(&key, 0, sizeof(Il2CppTokenIndexMethodTuple));
        key.token = method->token;

        const Il2CppTokenIndexMethodTuple* res = (const Il2CppTokenIndexMethodTuple*)bsearch(&key, codeGenModule->reversePInvokeWrapperIndices, codeGenModule->reversePInvokeWrapperCount, sizeof(Il2CppTokenIndexMethodTuple), CompareIl2CppTokenIndexMethodTuple);

        if (res == NULL)
            return NULL;

        uint32_t index = res->index;

        assert(index < g_CodeRegistration.reversePInvokeWrapperCount);
        return g_CodeRegistration.reversePInvokeWrappers[index];
    }

    intptr_t PlatformInvoke::MarshalDelegate(MonoDelegate* d)
    {
        if (d == NULL)
            return 0;

        if (unity_mono_method_is_inflated(d->method))
            mono_raise_exception(mono_get_exception_not_supported("IL2CPP does not support marshaling delegates that point to generic methods."));

        Il2CppMethodPointer reversePInvokeWrapper = GetReversePInvokeWrapperFromIndex((MonoMethod*)d->method);
        if (reversePInvokeWrapper == NULL)
        {
            // Okay, we cannot marshal it for some reason. Figure out why.
            if (mono_signature_is_instance(mono_method_signature((MonoMethod*)d->method)))
                mono_raise_exception(mono_get_exception_not_supported("IL2CPP does not support marshaling delegates that point to instance methods to native code."));

            mono_raise_exception(mono_get_exception_not_supported("To marshal a managed method, please add an attribute named 'MonoPInvokeCallback' to the method definition."));
        }

        return reinterpret_cast<intptr_t>(reversePInvokeWrapper);
    }

    Il2CppDelegate* PlatformInvoke::MarshalFunctionPointerToDelegate(void* functionPtr, MonoClass* delegateType)
    {
        if (!mono_unity_class_has_parent_unsafe(delegateType, mono_unity_defaults_get_delegate_class()))
            mono_raise_exception(mono_get_exception_argument("t", "Type must derive from Delegate."));

        if (mono_class_is_generic(delegateType) || mono_class_is_inflated(delegateType))
            mono_raise_exception(mono_get_exception_argument("t", "The specified Type must not be a generic type definition."));

        const Il2CppInteropData* interopData = FindInteropDataFor(delegateType);
        Il2CppMethodPointer managedToNativeWrapperMethodPointer = interopData != NULL ? interopData->delegatePInvokeWrapperFunction : NULL;

        if (managedToNativeWrapperMethodPointer == NULL)
            mono_raise_exception(mono_unity_exception_get_marshal_directive(il2cpp::utils::StringUtils::Printf("Cannot marshal P/Invoke call through delegate of type '%s.%s'", mono_class_get_namespace(delegateType), mono_class_get_name(delegateType)).c_str()));

        MonoObject* delegate = mono_object_new(g_MonoDomain, delegateType);
        Il2CppMethodPointer nativeFunctionPointer = (Il2CppMethodPointer)functionPtr;

        const MonoMethod* method = il2cpp::utils::NativeDelegateMethodCache::GetNativeDelegate(nativeFunctionPointer);
        if (method == NULL)
        {
            MonoMethod* newMethod = mono_unity_method_delegate_invoke_wrapper(delegateType);
            mono_unity_method_set_method_pointer(newMethod, (void*)nativeFunctionPointer);
            mono_unity_method_set_invoke_pointer(newMethod, NULL);
            il2cpp::utils::NativeDelegateMethodCache::AddNativeDelegate(nativeFunctionPointer, newMethod);
            method = newMethod;
        }

        MonoError unused;
        // FIXME: do we need to free these handles?
        MonoObjectHandle thisHandle = MONO_HANDLE_NEW(MonoObject, (MonoObject*)delegate);
        MonoObjectHandle targetHandle = MONO_HANDLE_NEW(MonoObject, (MonoObject*)delegate);
        gboolean success = mono_delegate_ctor_with_method(thisHandle, targetHandle, (void*)managedToNativeWrapperMethodPointer, const_cast<MonoMethod*>(method), &unused);

        return (Il2CppDelegate*)delegate;
    }
} // namespace vm
} // namespace mono
