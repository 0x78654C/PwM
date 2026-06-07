namespace PwM.Mobile.Services;

/// <summary>
/// Persists app settings via MAUI Preferences (replaces Windows Registry).
/// </summary>
public class SettingsService
{
    private const string KeyIterations = "Argon2Iterations";
    private const string KeyMemorySize = "Argon2MemorySize";
    private const string KeyParallelism = "Argon2Parallelism";
    private const string KeyAutoLockMinutes = "AutoLockMinutes";

    public int Argon2Iterations
    {
        get => Preferences.Default.Get(KeyIterations, PwMLib.GlobalVariables.argon2Iterations);
        set
        {
            PwMLib.GlobalVariables.argon2Iterations = value;
            Preferences.Default.Set(KeyIterations, value);
        }
    }

    public int Argon2MemorySize
    {
        get => Preferences.Default.Get(KeyMemorySize, PwMLib.GlobalVariables.argon2MemorySize);
        set
        {
            PwMLib.GlobalVariables.argon2MemorySize = value;
            Preferences.Default.Set(KeyMemorySize, value);
        }
    }

    public int Argon2Parallelism
    {
        get => Preferences.Default.Get(KeyParallelism, PwMLib.GlobalVariables.argon2Parallelism);
        set
        {
            PwMLib.GlobalVariables.argon2Parallelism = value;
            Preferences.Default.Set(KeyParallelism, value);
        }
    }

    public int AutoLockMinutes
    {
        get => Preferences.Default.Get(KeyAutoLockMinutes, 10);
        set => Preferences.Default.Set(KeyAutoLockMinutes, value);
    }

    /// <summary>
    /// Loads stored settings into PwMLib global state. Call at startup.
    /// </summary>
    public void Apply()
    {
        PwMLib.GlobalVariables.argon2Iterations = Argon2Iterations;
        PwMLib.GlobalVariables.argon2MemorySize = Argon2MemorySize;
        PwMLib.GlobalVariables.argon2Parallelism = Argon2Parallelism;
    }
}
