using System;

public class CircularBuffer<T> {
    private readonly T[] buffer;
    private readonly Action<T> onOverwrite;
    private int head; // Index of the oldest element.
    private int count;

    public int Capacity => buffer.Length;
    public int Count => count;

    public CircularBuffer(int capacity, Action<T> onOverwrite = null) {
        buffer = new T[capacity];
        this.onOverwrite = onOverwrite;
        head = 0;
        count = 0;
    }

    public void Add(T item) {
        if (count < Capacity) {
            int index = (head + count) % Capacity;
            buffer[index] = item;
            count++;
        }
        else {
            onOverwrite?.Invoke(buffer[head]);
            buffer[head] = item;
            head = (head + 1) % Capacity;
        }
    }

    public T[] ToArray() {
        T[] array = new T[count];
        for (int i = 0; i < count; i++) {
            array[i] = buffer[(head + i) % Capacity];
        }

        return array;
    }

    public void Clear() {
        for (int i = 0; i < count; i++) {
            int index = (head + i) % Capacity;
            onOverwrite?.Invoke(buffer[index]);
            buffer[index] = default;
        }

        head = 0;
        count = 0;
    }
}