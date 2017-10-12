using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TreasureHunter.Common.TransactionObjects
{
    public class PaymentNotificationMessage
    {
        public Guid PaymentId { get; private set; }

        public PaymentNotificationMessage(Guid id)
        {
            PaymentId = id;
        }
    }
}
