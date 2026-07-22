namespace WobbleStack.Domain
{
    public sealed class SeededRandom
    {
        private uint _state;

        public SeededRandom(uint seed)
        {
            _state = seed;
        }

        public float NextFloat()
        {
            _state += 0x6D2B79F5u;
            uint value = _state;
            value = unchecked((value ^ (value >> 15)) * (value | 1u));
            value ^= value + unchecked((value ^ (value >> 7)) * (value | 61u));
            return ((value ^ (value >> 14)) & 0xFFFFFFFFu) / 4294967296f;
        }
    }
}
