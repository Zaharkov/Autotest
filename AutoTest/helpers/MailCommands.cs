using System;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using AE.Net.Mail;
using MailMessage = System.Net.Mail.MailMessage;

namespace AutoTest.helpers
{
    /// <summary>
    /// Класс для работы с почтовым сервисом
    /// </summary>
    static class MailCommands
    {
        public static MailManager Google = new MailManager(HostType.Google);
        public static MailManager Action = new MailManager(HostType.Action);

        public enum HostType
        {
            Google,
            Action
        }

        public class MailManager
        {
            /// <summary>
            /// Клиент, выполняющий запросы к почтовому серверу
            /// Каждый раз инициализирует новый объект
            /// По умолчанию дожидается процесса очистки почты
            /// </summary>
            private ImapClient ImapClient
            {
                get
                {
                    if (_waitSweep != null)
                    {
                        _waitSweep.Wait();
                        _waitSweep = null;
                    }

                    return new ImapClient(_host, _login, _pass, AuthMethods.Login, _port, _ssl, _ssl);
                }
            }

            private string _host;
            private string _login;
            private string _pass;
            private int _port;
            private bool _ssl;

            private Task _waitSweep;

            /// <summary>
            /// Создание подключения к почте
            /// По умолчанию задана тестовая почта "autotest@action-media.ru"
            /// </summary>
            public MailManager(HostType type)
            {
                if(type == HostType.Action)
                    ActionInit();

                if(type == HostType.Google)
                    GoogleInit();
            }

            /// <summary>
            /// Параметры для внешней почты
            /// </summary>
            private void GoogleInit()
            {
                var host = ParametersInit.GoogleHost;
                var login = ParametersInit.GoogleLogin;
                var pass = ParametersInit.GooglePass;

                _host = host;
                _login = login;
                _pass = pass;
                _port = 993;
                _ssl = true;
            }

            /// <summary>
            /// Параметры для внутренней почты
            /// </summary>
            private void ActionInit()
            {
                var host = ParametersInit.MailHost;
                var login = ParametersInit.MailLogin;
                var pass = ParametersInit.MailPass;

                _host = host;
                _login = login;
                _pass = pass;
                _port = 143;
                _ssl = false;
            }

            /// <summary>
            /// Получить список писем
            /// </summary>
            /// <param name="mail">какой адрес должен содержаться</param>
            /// <param name="needText">как текст в письме должен содержаться</param>
            /// <returns></returns>
            private AE.Net.Mail.MailMessage[] GetMails(string mail, string needText = "")
            {
                using (var client = ImapClient)
                {
                    var messages = client.GetMessages(0, client.GetMessageCount(), false);

                    if (messages != null)
                        return messages.Where(t => t.From.Address.Contains(mail))
                            .Where(t => t.Body.Contains(needText)).ToArray();

                    return new AE.Net.Mail.MailMessage[0];
                }
            }

            /// <summary>
            /// Удаляет сообщения для определенного мыла
            /// </summary>
            /// <param name="mail">Мыло</param>
            /// <param name="needText">Если задан, будет искать сообщения содержащие данный текст</param>
            public void DeleteMailFromLogin(string mail, string needText = "")
            {
                var messages = GetMails(mail, needText);

                using (var client = ImapClient)
                {
                    foreach (var message in messages)
                    {
                        client.DeleteMessage(message);
                        client.Expunge();
                    }
                }
            }

            /// <summary>
            /// Проверка, что на почте есть сообщение от определенного мыла
            /// </summary>
            /// <param name="mail">Мыло</param>
            /// <param name="needText">Если задан, будет искать сообщения содержащие данный текст</param>
            /// <returns>Да/Нет</returns>
            public bool CheckMail(string mail, string needText = "")
            {
                var messages = GetMails(mail, needText);
                return messages.Any();
            }

            /// <summary>
            /// Удаляет все сообщения
            /// </summary>
            public void SweepMail()
            {
                var client = ImapClient;
                
                _waitSweep = Task.Run(() =>
                {
                    var messages = client.GetMessages(0, client.GetMessageCount(), false);

                    if (messages == null)
                        return;

                    foreach (var message in messages)
                    {
                        client.DeleteMessage(message);
                        client.Expunge();
                    }

                    client.Dispose();
                });
            }

            /// <summary>
            /// Получить текст писем с определенного мыла
            /// </summary>
            /// <param name="mail">Мыло</param>
            /// <param name="needText">Если задан, будет искать сообщения содержащие данный текст</param>
            /// <returns>Массив из текстов писем</returns>
            public string[] GetMailsFromLogin(string mail, string needText = "")
            {
                var messages = GetMails(mail, needText);
                var result = new string[messages.Count()];

                for (var i = 0; i < messages.Count(); i++)
                    result[i] = messages[i].Body;

                return result;
            }

            /// <summary>
            /// Получить число писем с определенного мыла
            /// </summary>
            /// <param name="mail">Мыло</param>
            /// <param name="needText">Если задан, будет искать сообщения содержащие данный текст</param>
            /// <returns>Число писем</returns>
            public int GetNumberMailsFromLogin(string mail, string needText = "")
            {
                var messages = GetMails(mail, needText);
                return messages.Count();
            }

            /// <summary>
            /// Отправляет результаты полного цикла письмом
            /// </summary>
            /// <param name="address">адрес</param>
            /// <param name="body">результаты тестов</param>
            public void SendTestsResult(string address, string body)
            {
                var mailMassage = new MailMessage
                {
                    From = new MailAddress(_login, "Автотесты"),
                    Subject = "Результаты цикла " + DateTime.Now.ToShortDateString(),
                    Body = body,
                    IsBodyHtml = false
                };
                mailMassage.To.Add(address);

                using (var client = new SmtpClient(_host) { Port = 25, UseDefaultCredentials = true })
                {
                    client.Send(mailMassage);
                }
            }

            /// <summary>
            /// Отправляет письмо о начале полного цикла
            /// </summary>
            /// <param name="address">адрес</param>
            /// <param name="body">сообщение</param>
            public void SendStartTests(string address, string body)
            {
                var mailMassage = new MailMessage
                {
                    From = new MailAddress(_login, "Автотесты"),
                    Subject =
                        "Запуск тестов в " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString(),
                    Body = body,
                    IsBodyHtml = false
                };
                mailMassage.To.Add(address);

                using (var client = new SmtpClient(_host) { Port = 25, UseDefaultCredentials = true })
                {
                    client.Send(mailMassage);
                }
            }
        }
    }
}
