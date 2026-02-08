namespace ARSandbox.Core
{
    public enum CamView { Top, Perspective, Side }

    public static class CameraStateLogic
    {
        public static CamView GetNextView(CamView current, int direction)
        {
            int next = (int)current + direction;
            int max = System.Enum.GetValues(typeof(CamView)).Length - 1;

            if (next < 0) next = max;
            else if (next > max) next = 0;

            return (CamView)next;
        }
    }
}
