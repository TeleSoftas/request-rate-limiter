using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace RequestRateLimiter.Core
{
    internal class CircularBufferCounter
	{

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		private struct SecondCount
		{
			public SecondCount(long ticks, int count)
			{
				Ticks = ticks;
				Count = count;
			}
			public int Count;
			public long Ticks;

			public override string ToString()
			{
				return new DateTime(Ticks).ToString(CultureInfo.InvariantCulture) + ": " + Count;
			}
		}

		private readonly object _syncRoot = new object();
		private SecondCount[] _buffer;
		private int _head;
		private int _tail;
		private int _elementCount;
		private int _total = 0;

		public DateTime StartTime { get; private set; }
		public string Name { get; set; }
		public TimeSpan Capacity { get; private set; }
		public TimeSpan Granularity { get; private set; }

        public CircularBufferCounter(DateTime startTime, TimeSpan granularity, TimeSpan capacity)
		{
			if (capacity.TotalSeconds < 1)
				throw new ArgumentOutOfRangeException("capacity", "Capacity must be more than a second");
			if (granularity.TotalMilliseconds < 100)
				throw new ArgumentOutOfRangeException("granularity", "Granuality must be more than a 100ms");
			if (granularity >= capacity)
				throw new ArgumentOutOfRangeException("granularity", "Granuality must be less then Capacity");

			Granularity = granularity;
			Capacity = capacity;
			StartTime = StartTime;
			long remainder;
			int count = (int)Math.DivRem(capacity.Ticks, granularity.Ticks, out remainder);
			if (remainder > 0)
				count++;
			_buffer = new SecondCount[count];
			_head = count - 1;
		}

        public void Increment(DateTime dateTime, int count = 1)
        {
            Interlocked.Add(ref _total, count);
            var ticks = dateTime.Ticks;
            if (_buffer[_head].Ticks / Granularity.Ticks == ticks / Granularity.Ticks)
            {
                _buffer[_head].Count += count;
            }
            else
            {
                lock (_syncRoot)
                {
                    if (_buffer[_head].Ticks / Granularity.Ticks != ticks / Granularity.Ticks)
                    {
                        _head = (_head + 1) % _buffer.Length;
                        _buffer[_head] = new SecondCount(ticks, count);
                        if (_elementCount == _buffer.Length)
                            _tail = (_tail + 1) % _buffer.Length;
                        else
                            ++_elementCount;
                    }
                }
            }
        }

		public long Count(DateTime from, TimeSpan span)
        {
            var to = from.Ticks + span.Ticks;
            if (to <= 0)
                to = TimeSpan.MaxValue.Ticks;
            int sum = 0;

            lock (_syncRoot)
            {
                for (long i = 0; i < _elementCount; i++)
                {
                    var sCount = this[i];
                    if (sCount.Ticks >= from.Ticks && sCount.Ticks < to)
                    {
                        sum += sCount.Count;
                    }
                    else if (sCount.Ticks > to)
                    {
                        break;
                    }

                }
                return sum;
            }
        }

        public long Count()
        {
            int sum = 0;
            for (long i = 0; i < _buffer.Length; i++)
            {
                sum += _buffer[i].Count;
            }
            return sum;
        }

        public long Count(TimeSpan interval)
        {
            return Count(DateTime.UtcNow.Subtract(interval), TimeSpan.MaxValue);
        }

		private SecondCount this[long index]
		{
			get
			{
				if (index < 0 || index >= _elementCount)
					throw new ArgumentOutOfRangeException("index");
				return _buffer[(_tail + index) % _buffer.Length];
			}
			set
			{
				if (index < 0 || index >= _elementCount)
					throw new ArgumentOutOfRangeException("index");

				_buffer[(_tail + index) % _buffer.Length] = value;
			}
		}
	}
}
