namespace Core.Models;

public class Block
{
    public uint BlockNum { get; }
    public int Hash { get; set; }
    public int PrevHash { get; }
    public uint Nonce { get; set; }
    public string Data { get; }
    public string AuthorAddress { get; }

    public Block(int prevHash, string data, uint blockNum, string authorAddress)
    {
        PrevHash = prevHash;
        Data = data;
        BlockNum = blockNum;
        AuthorAddress = authorAddress;
    }


    public override string ToString()
    {
        return $"\n\tAuthor: {AuthorAddress}\n\tBlockNum: {BlockNum}\n\tHash: {Hash}\n\tPrevHash: {PrevHash}\n\tNonce: {Nonce}\n\tData: {Data}\n\t";
    }
}
