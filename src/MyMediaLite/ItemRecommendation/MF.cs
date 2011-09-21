// Copyright (C) 2010 Steffen Rendle, Zeno Gantner, Christoph Freudenthaler
// Copyright (C) 2011 Zeno Gantner
//
// This file is part of MyMediaLite.
//
// MyMediaLite is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// MyMediaLite is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with MyMediaLite.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using MyMediaLite.DataType;
using MyMediaLite.Taxonomy;
using MyMediaLite.IO;

namespace MyMediaLite.ItemRecommendation
{
	/// <summary>Abstract class for matrix factorization based item predictors</summary>
	public abstract class MF : IncrementalItemRecommender, IIterativeModel
	{
		/// <summary>Latent user factor matrix</summary>
		protected Matrix<double> user_factors;
		/// <summary>Latent item factor matrix</summary>
		protected Matrix<double> item_factors;

		/// <summary>Mean of the normal distribution used to initialize the latent factors</summary>
		public double InitMean { get; set; }

		/// <summary>Standard deviation of the normal distribution used to initialize the latent factors</summary>
		public double InitStdDev { get; set; }

		/// <summary>Number of latent factors per user/item</summary>
		public uint NumFactors { get { return (uint) num_factors; } set { num_factors = (int) value; } }
		/// <summary>Number of latent factors per user/item</summary>
		protected int num_factors = 10;

		/// <summary>Number of iterations over the training data</summary>
		public uint NumIter { get; set; }

		/// <summary>Default constructor</summary>
		public MF()
		{
			NumIter = 30;
			InitMean = 0;
			InitStdDev = 0.1;
		}

		///
		protected virtual void InitModel()
		{
			user_factors = new Matrix<double>(MaxUserID + 1, NumFactors);
			item_factors = new Matrix<double>(MaxItemID + 1, NumFactors);

			MatrixUtils.InitNormal(user_factors, InitMean, InitStdDev);
			MatrixUtils.InitNormal(item_factors, InitMean, InitStdDev);
		}

		///
		public override void Train()
		{
			InitModel();

			for (uint i = 0; i < NumIter; i++)
				Iterate();
		}

		/// <summary>Iterate once over the data</summary>
		public abstract void Iterate();

		/// <summary>Computes the fit (optimization criterion) on the training data</summary>
		/// <returns>a double representing the fit, lower is better</returns>
		public abstract double ComputeFit();

		/// <summary>Predict the weight for a given user-item combination</summary>
		/// <remarks>
		/// If the user or the item are not known to the recommender, zero is returned.
		/// To avoid this behavior for unknown entities, use CanPredict() to check before.
		/// </remarks>
		/// <param name="user_id">the user ID</param>
		/// <param name="item_id">the item ID</param>
		/// <returns>the predicted weight</returns>
		public override double Predict(int user_id, int item_id)
		{
			if ((user_id < 0) || (user_id >= user_factors.dim1))
				return 0;
			if ((item_id < 0) || (item_id >= item_factors.dim1))
				return 0;

			return MatrixUtils.RowScalarProduct(user_factors, user_id, item_factors, item_id);
		}

		///
		public override void SaveModel(string file)
		{
			using ( StreamWriter writer = Model.GetWriter(file, this.GetType()) )
			{
				IMatrixUtils.WriteMatrix(writer, user_factors);
				IMatrixUtils.WriteMatrix(writer, item_factors);
			}
		}

		///
		public override void LoadModel(string file)
		{
			using ( StreamReader reader = Model.GetReader(file, this.GetType()) )
			{
				var user_factors = (Matrix<double>) IMatrixUtils.ReadMatrix(reader, new Matrix<double>(0, 0));
				var item_factors = (Matrix<double>) IMatrixUtils.ReadMatrix(reader, new Matrix<double>(0, 0));

				if (user_factors.NumberOfColumns != item_factors.NumberOfColumns)
					throw new IOException(
									string.Format("Number of user and item factors must match: {0} != {1}",
												  user_factors.NumberOfColumns, item_factors.NumberOfColumns));

				this.MaxUserID = user_factors.NumberOfRows - 1;
				this.MaxItemID = item_factors.NumberOfRows - 1;

				// assign new model
				if (this.NumFactors != user_factors.NumberOfColumns)
				{
					Console.Error.WriteLine("Set num_factors to {0}", user_factors.NumberOfColumns);
					this.NumFactors = (uint) user_factors.NumberOfColumns;
				}
				this.user_factors = user_factors;
				this.item_factors = item_factors;
			}
		}
	}
}