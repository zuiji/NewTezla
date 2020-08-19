using System;
using System.Diagnostics.CodeAnalysis;

namespace NewTezla
{
    class Program
    {
        static void Main(string[] args)
        {
            Car car = new Car();

            do
            {
                car.State = GetAnswers.GetChoiceFromEnum<CarState>($"what will you do {CarState.PowerOn} {CarState.PowerOff}");

                switch (car.State)
                {
                    case CarState.PowerOn:
                        Console.WriteLine("Car is running");
                        car.State = GetAnswers.GetChoiceFromEnum<CarState>($"what will you do {CarState.PowerOff} {CarState.Speeding} {CarState.Breaking}");
                        switch (car.State)
                        {
                            case CarState.PowerOff:
                                Console.WriteLine("Car turned off ");
                                break;
                            case CarState.Speeding:
                                Console.WriteLine("Car is speeding");
                                break;
                            case CarState.Breaking:
                                Console.WriteLine("Car is breaking");
                                break;
                        }
                        break;
                    
                    default:
                        Console.WriteLine("Car turned off ");
                        break;
                }
            } while (car.State != CarState.PowerOff);
        }
    }
}


