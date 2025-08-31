using System;
using System.Collections;
using System.Collections.Generic;

namespace PROG7312
{
    public class ReportItem
    {
        public string Location { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }

        public ReportItem(string location, string category, string description, string imagePath)
        {
            Location = location;
            Category = category;
            Description = description;
            ImagePath = imagePath;
        }

        public override string ToString()
        {
            return $"{Category}, {Location}, {Description}, {ImagePath}";
        }
    }

    public class ReportQueue : IEnumerable<ReportItem>
    {
        private List<ReportItem> items;

        public ReportQueue()
        {
            items = new List<ReportItem>();
        }

        // Add to the end (enqueue)
        public void Enqueue(ReportItem item)
        {
            items.Add(item);
        }

        // Remove from the start (dequeue)
        public ReportItem Dequeue()
        {
            if (items.Count == 0)
                throw new InvalidOperationException("Queue is empty.");

            ReportItem first = items[0];
            items.RemoveAt(0);
            return first;
        }

        // Look at first item without removing
        public ReportItem Peek()
        {
            if (items.Count == 0)
                throw new InvalidOperationException("Queue is empty.");

            return items[0];
        }

        public int Count => items.Count;

        public IEnumerator<ReportItem> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
