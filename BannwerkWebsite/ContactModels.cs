using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using WebsiteBannwerk.Resources.Main;
using WebsiteBannwerk.Resources.Contact;

namespace WebsiteBannwerk.Models
{
    public class ContactModel
    {
        [Required(ErrorMessageResourceType = typeof(ErrorMessages),
            ErrorMessageResourceName = "FieldRequired")]
        [StringLength(100, MinimumLength = 1, ErrorMessageResourceType = typeof(ErrorMessages),
            ErrorMessageResourceName = "InputOutOfBounds")]
        public string UserName { get; set; }

        [Required(ErrorMessageResourceType = typeof(ErrorMessages),
            ErrorMessageResourceName = "FieldRequired")]
        [EmailAddress(ErrorMessageResourceType = typeof(ErrorMessages),
            ErrorMessageResourceName = "NoEmailAddress", 
            ErrorMessage = null /*required due to known mvc bug*/)]
        [StringLength(100, MinimumLength = 1, ErrorMessageResourceType = typeof(ErrorMessages),
            ErrorMessageResourceName = "InputOutOfBounds")]
        public string EMail { get; set; }

        [Required(ErrorMessageResourceType = typeof(ErrorMessages),
            ErrorMessageResourceName = "FieldRequired")]
        [StringLength(100, MinimumLength = 10, ErrorMessageResourceType = typeof(ErrorMessages),
            ErrorMessageResourceName = "InputOutOfBounds")]
        public string Text { get; set; }
    }
}