﻿using Samples.WriteModel.Aggregate;
using Samples.WriteModel.Commands;
using ServiceStack.EventStore.Extensions;

namespace Samples.WriteModel.Services
{
    using System.Threading.Tasks;
    using ServiceStack;
    using ServiceStack.EventStore.Repository;

    namespace Samples.AWriteModel.ServiceInterface
    {
        public class PurchaseOrderService : Service
        {
            public IEventStoreRepository EventStore { get; set; }

            public async Task<object> Any(CreatePurchaseOrder cmd)
            {
                await new PurchaseOrder()
                            .AddLineItems(cmd.OrderLineItems)
                            .SaveAsync(EventStore)
                            .ConfigureAwait(false);

                return Task.FromResult(true);
            }

            public async Task<object> Any(UpdateOrderStatus cmd)
            {
                var order = await EventStore.GetByIdAsync<PurchaseOrder>(cmd.OrderId)
                                            .ConfigureAwait(false);

                order.UpdateStatus(cmd.NewStatus);

                await EventStore.SaveAsync(order).ConfigureAwait(false);

                return Task.FromResult(true);
            }
        }
    }
}
