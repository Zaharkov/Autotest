namespace AutoTest.helpers.Parameters
{
    /// <summary>
    /// Класс параметров проекта автотестирования
    /// </summary>
    public class ParametersRead : ParametersFunctions
    {
        private ParametersRead()
        {
            
        }

        private static ParametersRead _parameters;
        private static readonly object LockObj = new object();

        public static ParametersRead Instance()
        {
            lock (LockObj)
            {
                return _parameters ?? (_parameters = new ParametersRead());
            }
        }

        public readonly ParamButton SiteExit = new ParamButton("//exit");
        public readonly ParamButton SiteIn = new ParamButton("//sitein");
        public readonly ParamButton In = new ParamButton("//in");
    }
}
