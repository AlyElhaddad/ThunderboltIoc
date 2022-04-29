namespace Thunderbolt.Extensions.SourceGenerators;

internal static class PathUtil
{
    internal static string ObjDir(string projDir)
    {
        return Path.Combine(projDir, "obj");
    }
    internal static string EmitDir(string projDir)
    {
        return Path.Combine(projDir, "obj", "thunderbolt_generator_temp");
    }
    internal static string TempProjDir(string projDir)
    {
        return Path.Combine(projDir, "obj", "thunderbolt_generator_temp", "thunderbolt_types_util_proj");
    }

    internal static void CreateTempDirs(string projDir)
    {
        string objdir = ObjDir(projDir);
        if (!Directory.Exists(objdir))
            Directory.CreateDirectory(objdir);

        string emitDir = EmitDir(projDir);
        if (!Directory.Exists(emitDir))
            Directory.CreateDirectory(emitDir);

        string tempProjDir = TempProjDir(projDir);
        if (!Directory.Exists(tempProjDir))
            Directory.CreateDirectory(tempProjDir);
    }

    internal static void DeleteTempDirs(string projDir)
    {
        string emitDir = Path.Combine(projDir, "obj", "thunderbolt_generator_temp");
        if (Directory.Exists(emitDir))
            Directory.Delete(emitDir, true);
    }
}
