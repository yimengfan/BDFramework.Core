using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BDFramework.Event
{
   public interface IEvent
   {
       void OnTrriger(IList<object> args);
   }
}
