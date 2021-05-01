namespace Libplanet.Net.Messages
{
	//FMONECHANGE -  changed message serialization from NetMQMessage to Bencoded message
    public interface IBenEncodeable
    {
        byte[] SerializeToBen();
        object FromBenBytes(byte[] bytes);
    }
}