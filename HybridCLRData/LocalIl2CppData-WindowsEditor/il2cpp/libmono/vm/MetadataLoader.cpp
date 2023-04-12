#include "il2cpp-config.h"
#include "MetadataLoader.h"
#include "os/File.h"
#include "os/Mutex.h"
#include "utils/PathUtils.h"
#include "utils/MemoryMappedFile.h"
#include "utils/StringUtils.h"
#include "utils/Runtime.h"

using namespace mono::vm;

static std::string s_DataDir;
static std::string s_DataDirFallback;

void* MetadataLoader::LoadMetadataFile(const char* fileName)
{
    std::string resourcesDirectory = il2cpp::utils::PathUtils::Combine(il2cpp::utils::Runtime::GetDataDir(), il2cpp::utils::StringView<char>("Metadata"));

    std::string resourceFilePath = il2cpp::utils::PathUtils::Combine(resourcesDirectory, il2cpp::utils::StringView<char>(fileName, strlen(fileName)));

    int error = 0;
    il2cpp::os::FileHandle* handle = il2cpp::os::File::Open(resourceFilePath, kFileModeOpen, kFileAccessRead, kFileShareRead, kFileOptionsNone, &error);
    if (error != 0)
        return NULL;

    void* fileBuffer = il2cpp::utils::MemoryMappedFile::Map(handle);

    il2cpp::os::File::Close(handle, &error);
    if (error != 0)
    {
        il2cpp::utils::MemoryMappedFile::Unmap(fileBuffer);
        fileBuffer = NULL;
        return NULL;
    }

    return fileBuffer;
}
