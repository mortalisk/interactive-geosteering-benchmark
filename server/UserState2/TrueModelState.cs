using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ResistivitySimulator;
using TrajectoryInterfaces;

namespace UserState
{
    public class TrueModelState
    {

        public const double DeviationModelGeneration = 2.5; //originally 2.5
        public const double ResestivityMeasureStd = 0.75; //originally 0.5; 0.75 works well


        public const double InstrumentSize = 3.0;
        private MultiDataGenerator _trueModel;
        public IEarthModelRealization TrueSubsurfaseModel1 { get; }


        public TrueModelState(int seed)
        {
            TrueSubsurfaseModel1 = _GenerateSyntheticTruthFromSeed(seed);
            _InitializeDataGenrator(TrueSubsurfaseModel1);
        }

        public TrueModelState(IEarthModelRealization syntheticTruth)
        {
            TrueSubsurfaseModel1 = syntheticTruth;
            _InitializeDataGenrator(TrueSubsurfaseModel1);
        }

        private IEarthModelRealization _GenerateSyntheticTruthFromSeed(int randomSeed)
        {
            var eManip = UserState.InitializeManipulator(randomSeed, DeviationModelGeneration);
            var model = eManip.Realizations[randomSeed % eManip.NumberOfRealizations];
            return model;
        }

        private void _InitializeDataGenrator(IResistivityModel earthmodel)
        {
            _trueModel = new MultiDataGenerator(earthmodel, InstrumentSize);
            _trueModel.dataError = ResestivityMeasureStd;
        }

        public ResistivityMeasurement GetData(IContinousState pos)
        {
            var v = _trueModel.GetData(pos);
            var fullMeasurement = new ResistivityMeasurement(pos, new ResistivityData2DFull(v),
                new ResistivityData2DFull(_trueModel.GetDataVariance()));
            return fullMeasurement;
        }
    }
}
