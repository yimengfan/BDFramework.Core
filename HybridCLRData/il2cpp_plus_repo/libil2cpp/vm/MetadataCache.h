#pragma once

#include <stdint.h>
#include <functional>
#include "il2cpp-config.h"
#include "Assembly.h"
#include "metadata/Il2CppTypeVector.h"
#include "il2cpp-class-internals.h"
#include "utils/dynamic_array.h"
#include "os/Mutex.h"

struct MethodInfo;
struct Il2CppClass;
struct Il2CppGenericContainer;
struct Il2CppGenericContext;
struct Il2CppGenericInst;
struct Il2CppGenericMethod;
struct Il2CppType;
struct Il2CppString;

namespace il2cpp
{
namespace vm
{
    struct RGCTXCollection
    {
        int32_t count;
        const Il2CppRGCTXDefinition* items;
    };

    enum PackingSize
    {
        Zero,
        One,
        Two,
        Four,
        Eight,
        Sixteen,
        ThirtyTwo,
        SixtyFour,
        OneHundredTwentyEight
    };

    const int kBitIsValueType = 1;
    const int kBitIsEnum = 2;
    const int kBitHasFinalizer = 3;
    const int kBitHasStaticConstructor = 4;
    const int kBitIsBlittable = 5;
    const int kBitIsImportOrWindowsRuntime = 6;
    const int kPackingSize = 7; // This uses 4 bits from bit 7 to bit 10
    const int kPackingSizeIsDefault = 11;
    const int kClassSizeIsDefault = 12;
    const int kSpecifiedPackingSize = 13; // This uses 4 bits from bit 13 to bit 16

    class LIBIL2CPP_CODEGEN_API MetadataCache
    {
    public:

        static void Register(const Il2CppCodeRegistration * const codeRegistration, const Il2CppMetadataRegistration * const metadataRegistration, const Il2CppCodeGenOptions* const codeGenOptions);

        static bool Initialize();
        static void InitializeGCSafe();
        static void InitializeAllMethodMetadata();

        static Il2CppClass* GetGenericInstanceType(Il2CppClass* genericTypeDefinition, const il2cpp::metadata::Il2CppTypeVector& genericArgumentTypes);
        static const MethodInfo* GetGenericInstanceMethod(const MethodInfo* genericMethodDefinition, const Il2CppGenericContext* context);
        static const MethodInfo* GetGenericInstanceMethod(const MethodInfo* genericMethodDefinition, const il2cpp::metadata::Il2CppTypeVector& genericArgumentTypes);
        static const Il2CppGenericContext* GetMethodGenericContext(const MethodInfo* method);
        static const Il2CppGenericContainer* GetMethodGenericContainer(const MethodInfo* method);
        static const MethodInfo* GetGenericMethodDefinition(const MethodInfo* method);

        static Il2CppClass* GetPointerType(Il2CppClass* type);
        static Il2CppClass* GetWindowsRuntimeClass(const char* fullName);
        static const char* GetWindowsRuntimeClassName(const Il2CppClass* klass);
        static Il2CppMethodPointer GetWindowsRuntimeFactoryCreationFunction(const char* fullName);
        static Il2CppClass* GetClassForGuid(const Il2CppGuid* guid);
        static void AddPointerType(Il2CppClass* type, Il2CppClass* pointerType);

        static const Il2CppGenericInst* GetGenericInst(const Il2CppType* const* types, uint32_t typeCount);
        static const Il2CppGenericInst* GetGenericInst(const il2cpp::metadata::Il2CppTypeVector& types);
        static const Il2CppGenericMethod* GetGenericMethod(const MethodInfo* methodDefinition, const Il2CppGenericInst* classInst, const Il2CppGenericInst* methodInst);

        static InvokerMethod GetInvokerMethodPointer(const MethodInfo* methodDefinition, const Il2CppGenericContext* context);
        static Il2CppMethodPointer GetMethodPointer(const MethodInfo* methodDefinition, const Il2CppGenericContext* context, bool adjustorThunk, bool methodPointer);

        static Il2CppClass* GetTypeInfoFromTypeIndex(TypeIndex index, bool throwOnError = true);
        static const Il2CppType* GetIl2CppTypeFromIndex(TypeIndex index);
        static const MethodInfo* GetMethodInfoFromIndex(EncodedMethodIndex index);
        static const Il2CppGenericMethod* GetGenericMethodFromIndex(GenericMethodIndex index);
        static Il2CppString* GetStringLiteralFromIndex(StringLiteralIndex index);
        static const char* GetStringFromIndex(StringIndex index);

        static FieldInfo* GetFieldInfoFromIndex(EncodedMethodIndex index);
        static void InitializeMethodMetadata(uint32_t index);

        static Il2CppMethodPointer GetAdjustorThunk(const Il2CppImage* image, uint32_t token);
        static Il2CppMethodPointer GetMethodPointer(const Il2CppImage* image, uint32_t token);
        static InvokerMethod GetMethodInvoker(const Il2CppImage* image, uint32_t token);
        static const Il2CppInteropData* GetInteropDataForType(const Il2CppType* type);
        static Il2CppMethodPointer GetReversePInvokeWrapper(const Il2CppImage* image, const MethodInfo* method);

        static Il2CppMethodPointer GetUnresolvedVirtualCallStub(const MethodInfo* method);

        static const Il2CppAssembly* GetAssemblyFromIndex(const Il2CppImage* image, AssemblyIndex index);
        static const Il2CppAssembly* GetAssemblyByName(const char* nameToFind);
        static Il2CppImage* GetImageFromIndex(ImageIndex index);
        static Il2CppClass* GetTypeInfoFromTypeDefinitionIndex(TypeDefinitionIndex index);
        static const Il2CppTypeDefinition* GetTypeDefinitionFromIndex(TypeDefinitionIndex index);
        static TypeDefinitionIndex GetExportedTypeFromIndex(TypeDefinitionIndex index);
        static const Il2CppGenericContainer* GetGenericContainerFromIndex(GenericContainerIndex index);
        static const Il2CppGenericParameter* GetGenericParameterFromIndex(GenericParameterIndex index);
        static const Il2CppType* GetGenericParameterConstraintFromIndex(GenericParameterConstraintIndex index);
        //static Il2CppClass* GetNestedTypeFromIndex(NestedTypeIndex index);
        static Il2CppClass* GetNestedTypeFromOffset(const Il2CppTypeDefinition* typeDefinition, NestedTypeIndex index);
        static const Il2CppType* GetInterfaceFromIndex(Il2CppClass* klass, InterfacesIndex index);
        static EncodedMethodIndex GetVTableMethodFromIndex(VTableIndex index);
        //static Il2CppInterfaceOffsetPair GetInterfaceOffsetIndex(InterfaceOffsetIndex index);
        static Il2CppInterfaceOffsetInfo GetInterfaceOffsetInfo(const Il2CppTypeDefinition* typeDefine, TypeInterfaceOffsetIndex index);
        static RGCTXCollection GetRGCTXs(const Il2CppImage* image, uint32_t token);
        static const Il2CppEventDefinition* GetEventDefinitionFromIndex(EventIndex index);
        static const Il2CppFieldDefinition* GetFieldDefinitionFromIndex(FieldIndex index);
        static const Il2CppFieldDefaultValue* GetFieldDefaultValueFromIndex(FieldIndex index);
        static const uint8_t* GetFieldDefaultValueDataFromIndex(FieldIndex index);
        static const Il2CppFieldDefaultValue* GetFieldDefaultValueForField(const FieldInfo* field);
        static const uint8_t* GetParameterDefaultValueDataFromIndex(ParameterIndex index);
        static const Il2CppParameterDefaultValue* GetParameterDefaultValueForParameter(const MethodInfo* method, const ParameterInfo* parameter);
        static int GetFieldMarshaledSizeForField(const FieldInfo* field);
        static const Il2CppMethodDefinition* GetMethodDefinitionFromIndex(MethodIndex index);
        static const MethodInfo* GetMethodInfoFromMethodDefinitionIndex(MethodIndex index);
        static const Il2CppPropertyDefinition* GetPropertyDefinitionFromIndex(PropertyIndex index);
        static const Il2CppParameterDefinition* GetParameterDefinitionFromIndex(Il2CppClass* klass, ParameterIndex index);
        static const Il2CppParameterDefinition* GetParameterDefinitionFromIndex(const Il2CppMethodDefinition* methodDef, ParameterIndex index);
        // returns the compiler computer field offset for type definition fields
        static int32_t GetFieldOffsetFromIndexLocked(TypeIndex typeIndex, int32_t fieldIndexInType, FieldInfo* field, const il2cpp::os::FastAutoLock& lock);
        static int32_t GetThreadLocalStaticOffsetForField(FieldInfo* field);
        static void AddThreadLocalStaticOffsetForFieldLocked(FieldInfo* field, int32_t offset, const il2cpp::os::FastAutoLock& lock);

        static int32_t GetReferenceAssemblyIndexIntoAssemblyTable(int32_t referencedAssemblyTableIndex);

        static const TypeDefinitionIndex GetIndexForTypeDefinition(const Il2CppClass* typeDefinition);
        static const TypeDefinitionIndex GetIndexForTypeDefinition(const Il2CppTypeDefinition* typeDef);
        static const GenericParameterIndex GetIndexForGenericParameter(const Il2CppGenericParameter* genericParameter);
        static const MethodIndex GetIndexForMethodDefinition(const MethodInfo* method);

        static CustomAttributeIndex GetCustomAttributeIndex(const Il2CppImage* image, uint32_t token);
        static CustomAttributesCache* GenerateCustomAttributesCache(CustomAttributeIndex index);
        static CustomAttributesCache* GenerateCustomAttributesCache(const Il2CppImage* image, uint32_t token);
        static bool HasAttribute(CustomAttributeIndex index, Il2CppClass* attribute);
        static bool HasAttribute(const Il2CppImage* image, uint32_t token, Il2CppClass* attribute);

        typedef void(*WalkTypesCallback)(Il2CppClass* type, void* context);
        static void WalkPointerTypes(WalkTypesCallback callback, void* context);

        static bool StructLayoutPackIsDefault(TypeDefinitionIndex index);
        static int32_t StructLayoutPack(TypeDefinitionIndex index);
        static bool StructLayoutSizeIsDefault(TypeDefinitionIndex index);
    private:
        static void InitializeUnresolvedSignatureTable();
        static void InitializeStringLiteralTable();
        static void InitializeGenericMethodTable();
        static void InitializeWindowsRuntimeTypeNamesTables();
        static void InitializeGuidToClassTable();
        static void IntializeMethodMetadataRange(uint32_t start, uint32_t count, const utils::dynamic_array<Il2CppMetadataUsage>& expectedUsages, bool throwOnError);
    public:
        static const Il2CppTypeDefinition* GetTypeHandleFromIndex(TypeDefinitionIndex typeIndex);
        static const Il2CppTypeDefinition* GetTypeDefinitionFromIl2CppType(const Il2CppType* type);
        static const Il2CppType* GetIl2CppTypeFromClass(const Il2CppClass* klass);
        static Il2CppClass* GetIl2CppClassFromTypeDefinition(const Il2CppTypeDefinition* typeDefinition);
        static const TypeDefinitionIndex GetTypeDefinitionIndexFromTypeDefinition(const Il2CppTypeDefinition* typeDefinition);
        static const MethodInfo* GetMethodInfoFromVTableSlot(const Il2CppClass* klass, int32_t vTableSlot);

        static const MethodInfo* GetMethodInfoFromMethodHandle(Il2CppMetadataMethodDefinitionHandle handle);
        static const Il2CppTypeDefinition* GetAssemblyTypeHandle(const Il2CppImage* image, int32_t index);
        static void RegisterInterpreterAssembly(Il2CppAssembly* assembly);
        static const Il2CppAssembly* LoadAssemblyFromBytes(const char* assemblyBytes, size_t length);
        static const Il2CppGenericMethod* FindGenericMethod(std::function<bool(const Il2CppGenericMethod*)> predic);
        static void FixThreadLocalStaticOffsetForFieldLocked(FieldInfo* field, int32_t offset, const il2cpp::os::FastAutoLock& lock);

        static const Il2CppFieldDefinition* GetFieldDefinitionFromTypeDefAndFieldIndex(const Il2CppTypeDefinition* typeDefinition, int32_t offset)
        {
            return GetFieldDefinitionFromIndex(typeDefinition->fieldStart + offset);
        }

        static const Il2CppGenericParameter* GetGenericParameterFromIndex(Il2CppMetadataGenericContainerHandle container, GenericParameterIndex offset)
        {
            return GetGenericParameterFromIndex(((Il2CppGenericContainer*)container)->genericParameterStart + offset);
        }
        
        static Il2CppMetadataTypeHandle GetNestedTypes(Il2CppMetadataTypeHandle handle, void** iter);
        static PackingSize ConvertPackingSizeToEnum(uint8_t packingSize);
        static Il2CppClass* FromTypeDefinition(TypeDefinitionIndex index);
        static Il2CppClass* GetTypeInfoFromHandle(Il2CppMetadataTypeHandle typeHandle)
        {
            return GetIl2CppClassFromTypeDefinition(typeHandle);
        }

        static const Il2CppType* GetInterfaceFromOffset(const Il2CppTypeDefinition* typeDefinition, InterfacesIndex index);
        static Il2CppMetadataGenericContainerHandle GetGenericContainerFromGenericClass(const Il2CppGenericClass* genericClass);
        static const Il2CppMethodDefinition* GetMethodDefinitionFromVTableSlot(const Il2CppTypeDefinition* typeDefinition, int32_t vTableSlot);
    };
} // namespace vm
} // namespace il2cpp
