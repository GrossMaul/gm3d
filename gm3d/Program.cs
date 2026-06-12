using Smash;

internal static class Program
{
    private static void Main(string[] args)
    {
        SmashEngine.Init();

        Application application = new App();
        application.Start();

        while (!application.ApplicationShouldClose())
        {
            SmashEngine.Update();

            application.Update(SmashEngine.DeltaTime);
            application.Render();
        }

        application.End();
        SmashEngine.Stop();
    }
}