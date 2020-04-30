using Bencodex.Types;
using FreeMarketOne.DataStructure.Objects.BaseItems;
using FreeMarketOne.Extensions.Helpers;
using Libplanet.Action;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FreeMarketOne.BlockChain.Actions
{
    public class BaseBlockChainAction : IBaseAction
    {
        private List<IBaseItem> memoryBaseItems { get; set; }
        private byte[] memorySerialized { get; set; }

        public BaseBlockChainAction()
        {
            memoryBaseItems = new List<IBaseItem>();
        }

        public List<IBaseItem> BaseItems
        {
            get
            {
                return memoryBaseItems;
            }
        }
        public void AddBaseItem(IBaseItem value)
        {
            memoryBaseItems.Add(value);

            var serializedItems = JsonConvert.SerializeObject(this.BaseItems);
            var compressedItems = ZipHelpers.Compress(serializedItems);

            memorySerialized = compressedItems;
        }

        public IValue PlainValue
        {
            get
            {
                return new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    { (Text)"items", new Binary(memorySerialized) },
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

            var serializedItems = ZipHelpers.Decompress(binaryValue.Value);
            memoryBaseItems = JsonConvert.DeserializeObject<List<IBaseItem>>(serializedItems);
            memorySerialized = binaryValue.Value;
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
