﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace TraktAPI.DataStructures
{
    [DataContract]
    public class TraktCredits
    {
        [DataMember(Name = "cast")]
        public List<TraktCharacter> Cast { get; set; }

        [DataMember(Name = "crew")]
        public TraktCrew Crew { get; set; }
    }
}
