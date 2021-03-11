namespace Libplanet.Net.Messages
{
    public interface IBenEncodeable
    {
        byte[] SerializeToBen();
        object FromBenBytes(byte[] bytes);
    }
}