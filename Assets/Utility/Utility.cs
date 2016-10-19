using UnityEngine;
using System.Collections;

public class Utility {
    // Swaps the values of its arguments.
    public static void Swap<T>(ref T a, ref T b) {
        T temp = a;
        a = b;
        b = temp;
    }
}
