﻿using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;

namespace AzureServiceCatalog.Web.Models
{
    public class NotificationProcessor
    {
        private const string htmlLineBreak = "<br />";
        public void SendFeedbackNotification(FeedbackViewModel model)
        {
            if (model != null)
            {
                var subject = string.Format(Config.FeedbackEmailSubjectFormat, model.Subject);
                var message = $"Name: {model.Name}{htmlLineBreak}Email: {model.Email}{htmlLineBreak}Subject: {model.Subject}{htmlLineBreak}Comments: {model.Comments}";

                SendEmailNotification(Config.AdminEmailAddress, subject, message);
            }
        }

        public void SendSupportNotification(EnrollmentSupportViewModel model)
        {
            if (model != null)
            {
                var subject = Config.SupportEmailSubjectFormat;
                var message = $"First Name: {model.FirstName}{htmlLineBreak}Last Name: {model.LastName}{htmlLineBreak}Company: {model.Company}{htmlLineBreak}Title: {model.Title}{htmlLineBreak}" + 
                              $"Email: {model.Email}{htmlLineBreak}Phone: {model.Phone}{htmlLineBreak}Comments: {model.Comments}";

                SendEmailNotification(Config.AdminEmailAddress, subject, message);
            }
        }

        public void SendActivationNotification(Organization org)
        {
            if (org != null)
            {
                var subject = $"New Activation: {org.VerifiedDomain}";
                var message = $"New Activation{htmlLineBreak}First Name: {ClaimsPrincipal.Current.FirstName()}{htmlLineBreak}Last Name: {ClaimsPrincipal.Current.LastName()}{htmlLineBreak}Email: {ClaimsPrincipal.Current.EmailAddress()}{htmlLineBreak}Org Domain: {org.VerifiedDomain}{htmlLineBreak}Date Enrolled: {org.EnrolledDate}{htmlLineBreak}";

                SendEmailNotification(Config.AdminEmailAddress, subject, message);
            }
        }

        private void SendEmailNotification(string sendToEmailAddress, string subject, string message)
        {
            if (string.IsNullOrEmpty(sendToEmailAddress))
            {
                string invalidEmailMessage = "Notification not sent. Email address not available";
                Trace.TraceError(string.Format(message));
                Trace.TraceError(invalidEmailMessage);
                throw new FormatException(invalidEmailMessage);
            }

            var sg = new SendGrid.SendGridAPIClient(Config.SendGridApiKey, Config.SendGridEndPoint);
            Email from = new Email(Config.NotificationFromEmailAddress, Config.NotificationFromName);
            Content content = new Content("text/plain", message.Replace(htmlLineBreak, Environment.NewLine));
            Content htmlContent = new Content("text/html", message);

            var to = new Email(sendToEmailAddress);
            var mail = new Mail(from, subject, to, content);
            mail.AddContent(htmlContent);
            mail.Subject = subject;

            var requestBody = mail.Get();

            dynamic response = sg.client.mail.send.post(requestBody: requestBody);

            HttpStatusCode statusCode = response.StatusCode;

            if (((int)statusCode >= 200) && ((int)statusCode <= 299))
            {
                Trace.TraceInformation("Email notification sent.");
            }
            else
            {
                Trace.TraceError(string.Format("Error sending email notification. {0}, {1}", statusCode, message));
                throw new HttpException ((int)statusCode, "Email could not be sent!");
            }
        }
    }
}