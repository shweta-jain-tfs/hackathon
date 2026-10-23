using System;
using CustomAction.Wrappers.Interfaces;

namespace CustomAction.Wrappers;

public class Date : IDate
{
    public DateTime Now => DateTime.Now;
}