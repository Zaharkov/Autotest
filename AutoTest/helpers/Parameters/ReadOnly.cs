namespace AutoTest.helpers
{
    /// <summary>
    /// Класс параметров проекта автотестирования
    /// </summary>
    public class Parameters : ParametersFunctions
    {
        private Parameters()
        {
            
        }

        private static Parameters _parameters;
        private static readonly object LockObj = new object();

        public static Parameters Instance()
        {
            lock (LockObj)
            {
                return _parameters ?? (_parameters = new Parameters());
            }
        }

        public readonly ParamButton SiteExit = new ParamButton("//exit");
        public readonly ParamButton SiteIn = new ParamButton("//sitein");
        public readonly ParamButton In = new ParamButton("//in");
    }
}
