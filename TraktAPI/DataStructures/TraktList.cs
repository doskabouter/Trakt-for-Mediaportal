﻿using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktList
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "description")]
        public string Description { get; set; }

        [DataMember(Name = "privacy")]
        public string Privacy { get; set; }

        [DataMember(Name = "display_numbers")]
        public bool DisplayNumbers { get; set; }

        [DataMember(Name = "allow_comments")]
        public bool AllowComments { get; set; }

        [DataMember(Name = "sort_by")]
        public bool SortBy { get; set; }

        [DataMember(Name = "sort_how")]
        public bool SortHow { get; set; }


    }
}
