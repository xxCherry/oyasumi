using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace oyasumi.Extensions
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> AppendRange<T>(this IEnumerable<T> self, IEnumerable<T> target)
		{
			foreach (var item in target)
			{
				self.Append(item);
			}

			return self;
		}
	}
}
