using System;
using DerpyNewbie.Common;
using UdonSharp;

namespace CenturionCC.System.Utils
{
    public static class CallbackUtil
    {
        /// <summary>
        /// Adds provided behaviour into provided array
        /// </summary>
        /// <param name="item">an item to insert into <c>arr</c></param>
        /// <param name="count">a count of current <c>arr</c>'s items</param>
        /// <param name="arr">an array which <c>item</c> gets inserted</param>
        /// <returns>true when it successfully added, false otherwise.</returns>
        public static bool AddBehaviour(UdonSharpBehaviour item,
            ref int count, ref UdonSharpBehaviour[] arr)
        {
            // Theres no behaviour to add
            if (item == null)
                return false;

            // Count should not be negative
            if (count < 0)
                count = 0;

            // Array should not be null
            if (arr == null)
                arr = new UdonSharpBehaviour[5];

            // Array should not contain multiple items
            if (arr.ContainsItem(item))
                return false;

            if (arr.Length <= count)
            {
                // arr.Length - count should be zero, but just to make sure!
                var newArr = new UdonSharpBehaviour[arr.Length + (arr.Length - count) + 5];
                Array.Copy(arr, newArr, arr.Length);
                arr = newArr;
            }

            arr[count] = item;
            ++count;
            return true;
        }

        /// <summary>
        /// Removes provided behaviour from provided array
        /// </summary>
        /// <param name="item">an item to remove</param>
        /// <param name="count">a count of current <c>arr</c>'s items</param>
        /// <param name="arr">an array which <c>item</c> will get removed from</param>
        /// <returns>true when it successfully removed, false otherwise.</returns>
        public static bool RemoveBehaviour(UdonSharpBehaviour item, ref int count, ref UdonSharpBehaviour[] arr)
        {
            if (item == null || arr == null)
                return false;

            var index = Array.IndexOf(arr, item);
            if (index == -1)
                return false;
            Array.ConstrainedCopy(arr, index + 1, arr, index, arr.Length - 1 - index);
            --count;
            return true;
        }
    }
}