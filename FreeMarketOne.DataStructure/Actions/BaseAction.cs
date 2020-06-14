using Bencodex.Types;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using Libplanet.Action;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace FreeMarketOne.DataStructure
{
    public class BaseAction : IBaseAction
    {
        private List<IBaseItem> _baseItems { get; set; }
        private byte[] _serialized { get; set; }

        public BaseAction()
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
            var compressedItems = ZipHelpers.Compress(serializedItems);

            _serialized = compressedItems;
        }

        public IValue PlainValue
        {
            get
            {
                if ((_serialized == null) || (_serialized.Length == 0))
                {
                    if (BaseItems.Any())
                    {
                        var serializedItems = JsonConvert.SerializeObject(BaseItems);
                        var compressedItems = ZipHelpers.Compress(serializedItems);

                        _serialized = compressedItems;
                    } 
                    else
                    {
                        return null;
                    }
                }

                return new Dictionary(new Dictionary<IKey, IValue>
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

            if ((binaryValue.Value != null) && (binaryValue.Value.Length > 0))
            {
                var serializedItems = ZipHelpers.Decompress(binaryValue.Value);
                _baseItems = JsonConvert.DeserializeObject<List<IBaseItem>>(serializedItems);
            }

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
