using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Bookproject.Models
{
    public class GuestResponse
    {
        [Required(ErrorMessage ="Please enret your name")]
        public string Name { get; set; }
        [Required(ErrorMessage ="Please enter your email adress")]
        [RegularExpression(".+\\@.+\\..+", ErrorMessage ="please enter valid email address")]
        public string Email { get; set; }
        [Required(ErrorMessage ="Please enter your phone number")]
        public string Phone { get; set; }
        [Required(ErrorMessage ="Please specity whether you'll attend")]
        public bool? WillAttend { get; set; }
    }
}
