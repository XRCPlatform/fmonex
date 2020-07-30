using Bencodex.Types;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using Libplanet.Action;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FreeMarketOne.DataStructure
{
    public class MarketAction : IBaseAction
    {
        private List<IBaseItem> _baseItems { get; set; }
        private byte[] _serialized { get; set; }

        public MarketAction()
        {
            _baseItems = new List<IBaseItem>();
        }

        public List<IBaseItem> BaseItems
        {
            get
            {
                return _baseItems;
            }
        }
        public void AddBaseItem(IBaseItem value)
        {
            _baseItems.Add(value);

            var serializedItems = JsonConvert.SerializeObject(this.BaseItems);
            var compressedItems = ZipHelper.Compress(serializedItems);

            _serialized = compressedItems;
        }

        public IValue PlainValue
        {
            get
            {
                return new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    { (Text)"items", new Binary(_serialized) },
                });
            }
        }

        public IAccountStateDelta Execute(IActionContext context)
        {
            return context.PreviousStates;
        }

        public void LoadPlainValue(IValue plainValue)
        {
            var dictionary = (Bencodex.Types.Dictionary)plainValue;
            var binaryValue = dictionary.GetValue<Binary>("items");

            var serializedItems = ZipHelper.Decompress(binaryValue.Value);
            _baseItems = JsonConvert.DeserializeObject<List<IBaseItem>>(serializedItems);
            _serialized = binaryValue.Value;
        }

        public void Render(IActionContext context, IAccountStateDelta nextStates)
        {
            // throw new NotImplementedException();
        }

        public void Unrender(IActionContext context, IAccountStateDelta nextStates)
        {
            //not used
        }
    }
}

