using Nop.Core.Events;
using Nop.Data;
using Nop.Plugin.API.ElisaIntegration.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.API.ElisaIntegration.Services
{
    public class CustomCartService
    {
        #region Fields
        private readonly IRepository<CustomCart> _customCartRepository;
        private readonly IRepository<CustomCartItems> _customCartItemsRepository;
        private readonly IEventPublisher _eventPublisher;
        #endregion

        #region Ctor
        public CustomCartService(IRepository<CustomCart> customCartRepository,
            IRepository<CustomCartItems> customCartItemsRepository,
            IEventPublisher eventPublisher)
        {
            _customCartRepository = customCartRepository;
            _customCartItemsRepository = customCartItemsRepository;
            _eventPublisher = eventPublisher;
        }

        #endregion

        #region Methods

        #region Custom cart
        public async Task DeleteCustomCartAsync(CustomCart cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            await _customCartRepository.DeleteAsync(cart);

            await _eventPublisher.EntityDeletedAsync(cart);
        }

        public async Task<CustomCart> GetCustomCartByElisaCartId(Guid elisaCartId)
        {
            if (elisaCartId == Guid.Empty)
                return null;

            var customCart = await (from ec in _customCartRepository.Table
                              where ec.ElisaCartId == elisaCartId
                              select ec).FirstOrDefaultAsync();

            return customCart;
        }

        public async Task<CustomCart> GetFirstCustomCartAsync()
        {
            return await _customCartRepository.Table.OrderBy(x => x.Id).FirstOrDefaultAsync();
        }

        public async Task InsertCustomCartAsync(CustomCart cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            await _customCartRepository.InsertAsync(cart);

            await _eventPublisher.EntityInsertedAsync(cart);
        }


        public async Task UpdateCustomCart(CustomCart cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            await _customCartRepository.UpdateAsync(cart);

            await _eventPublisher.EntityUpdatedAsync(cart);
        }

        #endregion

        #region Custom cart items
        public async Task DeleteCustomCartItemsAsync(IList<CustomCartItems> cartItems)
        {
            if (cartItems == null)
                throw new ArgumentNullException(nameof(cartItems));

            await _customCartItemsRepository.DeleteAsync(cartItems);
        }

        public IList<CustomCartItems> GetCustomCartItemsByCartId(Guid cartId)
        {
            if (cartId == Guid.Empty)
                return null;

            var items = (from ec in _customCartItemsRepository.Table
                         where ec.CustomCartId == cartId
                         select ec).ToList();

            return items;
        }

        public async Task InsertCustomCartItemsAsync(CustomCartItems cartItem)
        {
            if (cartItem == null)
                throw new ArgumentNullException(nameof(cartItem));

            await _customCartItemsRepository.InsertAsync(cartItem);
        }

        public async Task UpdateCustomCartItemsAsync(CustomCartItems cartItem)
        {
            if (cartItem == null)
                throw new ArgumentNullException(nameof(cartItem));

            await _customCartItemsRepository.UpdateAsync(cartItem);
        }
        #endregion

        #endregion
    }
}
