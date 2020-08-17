using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Linq;
using System.IO;
using MMR.Randomizer.Extensions;

namespace MMR.Randomizer
{
    
    public class Multiworld_item
    {

        public UInt16 start_id { get; }
        public UInt16 end_id { get; }
        public UInt16 player { get; }
        public UInt16 new_id { get;  }
        public UInt64 notice { get; }
        public Multiworld_item(UInt16 start, UInt16 end, UInt16 player_num, UInt16 temp = 0xABCD)
        {
            start_id = start;
            end_id = end;

            player = player_num;
            new_id = temp;
            notice = 0xCAFEBEEFCAFEBEEF;

        }
        public Multiworld_item()
        {

        }

        public byte[] ToArray()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(this.start_id);
            writer.Write(this.end_id);
            writer.Write(this.player);
            writer.Write(this.new_id);
            writer.Write(this.notice);

            return stream.ToArray();
        }
    }
    public class Multiworld_table : IEnumerable, IEnumerator
    {
        public List<Multiworld_item> entries = new List<Multiworld_item>();
        int position = -1;

        public Multiworld_table()
        {

        }
        public Multiworld_table(ItemList itemlist)
        {
            foreach ( var item in itemlist)
            {
                UInt16 new_get_item_index;
                if (item.NewLocation == null || item.NewLocation.Value.GetItemIndex() == null)
                    new_get_item_index = 0xFFFF;
                else
                    new_get_item_index = (UInt16)item.Item.GetItemIndex();


                UInt16 original_get_item_index;
                if (item.Item == null || item.Item.GetItemIndex() == null)
                    original_get_item_index = 0xFFFF;
                else
                    original_get_item_index = (UInt16)item.Item.GetItemIndex();

                UInt16 to_player = (UInt16)item.Mulitworld_player_id;
                Multiworld_item mitem = new Multiworld_item(original_get_item_index, new_get_item_index, to_player);
                entries.Add(mitem);
            }
        }
        //IEnumerator and IEnumerable require these methods.
        public IEnumerator GetEnumerator()
        {
            return (IEnumerator)this;
        }
        //IEnumerator
        public bool MoveNext()
        {
            position++;
            return (position < entries.Count);
        }
        //IEnumerable
        public void Reset()
        {
            position = 0;
        }
        //IEnumerable
        public object Current
        {
            get { return entries[position]; }
        }

        public void Add(Multiworld_item x)
        {
            entries.Add(x);
        }

    }
}
