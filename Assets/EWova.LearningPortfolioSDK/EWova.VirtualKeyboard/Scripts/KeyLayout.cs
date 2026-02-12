using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace EWova.VirtualKeyboard
{
    public class KeyLayout : MonoBehaviour, IEnumerable<Key>
    {
        public const int MaxVertical = 4;

        [SerializeField] private Key[] Line0;
        [SerializeField] private Key[] Line1;
        [SerializeField] private Key[] Line2;
        [SerializeField] private Key[] Line3;
        [SerializeField] private Key[] Line4;

        public Key[] this[int vertical]
        {
            get
            {
                return vertical switch
                {
                    0 => Line0,
                    1 => Line1,
                    2 => Line2,
                    3 => Line3,
                    4 => Line4,
                    _ => throw new System.IndexOutOfRangeException($"Vertical index must be between 0 and {MaxVertical}.")
                };
            }
        }

        public Key this[int vertical, int horizon]
        {
            get
            {
                Key[] line = vertical switch
                {
                    0 => Line0,
                    1 => Line1,
                    2 => Line2,
                    3 => Line3,
                    4 => Line4,
                    _ => throw new System.IndexOutOfRangeException($"Vertical index must be between 0 and {MaxVertical}.")
                };

                if (line.Length <= horizon || horizon < 0)
                    throw new System.IndexOutOfRangeException("Horizon index is out of range for the specified line.");

                return line[horizon];
            }
        }

        public bool TryGetKey(int vertical, int horizon, out Key key)
        {
            try
            {
                key = this[vertical, horizon];
                return true;
            }
            catch (System.IndexOutOfRangeException)
            {
                key = null;
                return false;
            }
        }
        public IEnumerator<Key> GetEnumerator()
        {
            for (int i = 0; i <= MaxVertical; i++)
            {
                foreach (var key in this[i])
                {
                    yield return key;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsVisible
        {
            get => gameObject.activeSelf;
            set => gameObject.SetActive(value);
        }
    }
}
