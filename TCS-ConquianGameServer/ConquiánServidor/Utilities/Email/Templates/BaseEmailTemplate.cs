using System;

namespace ConquiánServidor.Utilities.Email.Templates
{
    public abstract class BaseEmailTemplate : IEmailTemplate
    {
        public abstract string Subject { get; } 
        public string HtmlBody => BuildHtml();
       
        protected abstract string Title { get; }     
        protected abstract string Message { get; }   
        protected abstract string Code { get; }      
        protected abstract string Footer { get; }    


        private string BuildHtml()
        {
            string timeStamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            return $@"
                <!DOCTYPE html>
                <html>
                <body style='font-family: Arial, sans-serif; color: #333;'>
                    
                    <h2>{Title}</h2>
                    
                    <p>{Message}</p>
                    
                    <div style='background-color: #f0f0f0; padding: 15px; text-align: center; margin: 20px 0;'>
                        <strong style='font-size: 24px; letter-spacing: 3px;'>{Code}</strong>
                    </div>
                    
                    <p>{Footer}</p>
                    
                    <hr style='border: 0; border-top: 1px solid #eee; margin-top: 30px;'>
                    
                    <p style='font-size: 11px; color: #888; text-align: right;'>
                        Generado el: {timeStamp}
                    </p>
                </body>
                </html>";
        }
    }
}
