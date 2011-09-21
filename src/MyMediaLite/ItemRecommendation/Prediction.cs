// Copyright (C) 2010 Steffen Rendle, Zeno Gantner
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using MyMediaLite.Data;
using MyMediaLite.DataType;

namespace MyMediaLite.ItemRecommendation
{
	/// <summary>Class that contains static methods for item prediction</summary>
	public static class Prediction
	{
		/// <summary>Write item predictions (scores) for all users to a file</summary>
		/// <param name="recommender">the <see cref="IRecommender"/> to use for making the predictions</param>
		/// <param name="train">a user-wise <see cref="IPosOnlyFeedback"/> containing the items already observed</param>
		/// <param name="relevant_items">the list of candidate items</param>
		/// <param name="num_predictions">the number of items to return per user, -1 if there should be no limit</param>
		/// <param name="user_mapping">an <see cref="IEntityMapping"/> object for the user IDs</param>
		/// <param name="item_mapping">an <see cref="IEntityMapping"/> object for the item IDs</param>
		/// <param name="filename">the name of the file to write to</param>
		static public void WritePredictions(
			IRecommender recommender,
			IPosOnlyFeedback train,
			ICollection<int> relevant_items,
			int num_predictions,
			IEntityMapping user_mapping, IEntityMapping item_mapping,
			string filename)
		{
			using (var writer = new StreamWriter(filename))
				WritePredictions(recommender, train, relevant_items, num_predictions, user_mapping, item_mapping, writer);
		}

		/// <summary>Write item predictions (scores) to a file</summary>
		/// <param name="recommender">the <see cref="IRecommender"/> to use for making the predictions</param>
		/// <param name="train">a user-wise <see cref="IPosOnlyFeedback"/> containing the items already observed</param>
		/// <param name="relevant_users">a list of users to make recommendations for</param>
		/// <param name="relevant_items">the list of candidate items</param>
		/// <param name="num_predictions">the number of items to return per user, -1 if there should be no limit</param>
		/// <param name="user_mapping">an <see cref="IEntityMapping"/> object for the user IDs</param>
		/// <param name="item_mapping">an <see cref="IEntityMapping"/> object for the item IDs</param>
		/// <param name="filename">the name of the file to write to</param>
		static public void WritePredictions(
			IRecommender recommender,
			IPosOnlyFeedback train,
			IList<int> relevant_users,
			ICollection<int> relevant_items,
			int num_predictions,
			IEntityMapping user_mapping, IEntityMapping item_mapping,
			string filename)
		{
			using (var writer = new StreamWriter(filename))
				WritePredictions(recommender, train, relevant_users, relevant_items, num_predictions, user_mapping, item_mapping, writer);
		}

		/// <summary>Write item predictions (scores) for all users to a TextWriter object</summary>
		/// <param name="recommender">the <see cref="IRecommender"/> to use for making the predictions</param>
		/// <param name="train">a user-wise <see cref="IPosOnlyFeedback"/> containing the items already observed</param>
		/// <param name="relevant_items">the list of candidate items</param>
		/// <param name="num_predictions">the number of items to return per user, -1 if there should be no limit</param>
		/// <param name="user_mapping">an <see cref="IEntityMapping"/> object for the user IDs</param>
		/// <param name="item_mapping">an <see cref="IEntityMapping"/> object for the item IDs</param>
		/// <param name="writer">the <see cref="TextWriter"/> to write to</param>
		static public void WritePredictions(
			IRecommender recommender,
			IPosOnlyFeedback train,
			ICollection<int> relevant_items,
			int num_predictions,
			IEntityMapping user_mapping, IEntityMapping item_mapping,
			TextWriter writer)
		{
			var relevant_users = new List<int>(user_mapping.InternalIDs);
			WritePredictions(recommender, train, relevant_users, relevant_items, num_predictions, user_mapping, item_mapping, writer);
		}

		/// <summary>Write item predictions (scores) to a TextWriter object</summary>
		/// <param name="recommender">the <see cref="IRecommender"/> to use for making the predictions</param>
		/// <param name="train">a user-wise <see cref="IPosOnlyFeedback"/> containing the items already observed</param>
		/// <param name="relevant_users">a list of users to make recommendations for</param>
		/// <param name="relevant_items">the list of candidate items</param>
		/// <param name="num_predictions">the number of items to return per user, -1 if there should be no limit</param>
		/// <param name="user_mapping">an <see cref="IEntityMapping"/> object for the user IDs</param>
		/// <param name="item_mapping">an <see cref="IEntityMapping"/> object for the item IDs</param>
		/// <param name="writer">the <see cref="TextWriter"/> to write to</param>
		static public void WritePredictions(
			IRecommender recommender,
			IPosOnlyFeedback train,
			IList<int> relevant_users,
			ICollection<int> relevant_items,
			int num_predictions,
			IEntityMapping user_mapping, IEntityMapping item_mapping,
			TextWriter writer)
		{
			foreach (int user_id in relevant_users)
			{
				var ignore_items = train.UserMatrix[user_id];
				WritePredictions(recommender, user_id, relevant_items, ignore_items, num_predictions, user_mapping, item_mapping, writer);
			}
		}

		/// <summary>Write item predictions (scores) to a TextWriter object</summary>
		/// <param name="recommender">the <see cref="IRecommender"/> to use for making the predictions</param>
		/// <param name="user_id">the ID of the user to make recommendations for</param>
		/// <param name="relevant_items">the list of candidate items</param>
		/// <param name="ignore_items">a list of items for which no predictions should be made</param>
		/// <param name="num_predictions">the number of items to return per user, -1 if there should be no limit</param>
		/// <param name="user_mapping">an <see cref="IEntityMapping"/> object for the user IDs</param>
		/// <param name="item_mapping">an <see cref="IEntityMapping"/> object for the item IDs</param>
		/// <param name="writer">the <see cref="TextWriter"/> to write to</param>
		static public void WritePredictions(
			IRecommender recommender,
			int user_id,
			ICollection<int> relevant_items,
			ICollection<int> ignore_items,
			int num_predictions,
			IEntityMapping user_mapping, IEntityMapping item_mapping,
			TextWriter writer)
		{
			var score_list = new List<WeightedItem>();
			foreach (int item_id in relevant_items)
				score_list.Add( new WeightedItem(item_id, recommender.Predict(user_id, item_id)));

			score_list.Sort();
			score_list.Reverse();

			int prediction_count = 0;

			writer.Write("{0}\t[", user_mapping.ToOriginalID(user_id));
			foreach (var wi in score_list)
			{
				if (!ignore_items.Contains(wi.item_id) && wi.weight > double.MinValue)
				{
					if (prediction_count == 0)
						writer.Write("{0}:{1}", item_mapping.ToOriginalID(wi.item_id), wi.weight.ToString(CultureInfo.InvariantCulture));
					else
						writer.Write(",{0}:{1}", item_mapping.ToOriginalID(wi.item_id), wi.weight.ToString(CultureInfo.InvariantCulture));

					prediction_count++;
				}

				if (prediction_count == num_predictions)
					break;
			}
			writer.WriteLine("]");
		}

		/// <summary>predict items for a specific users</summary>
		/// <param name="recommender">the <see cref="IRecommender"/> object to use for the predictions</param>
		/// <param name="user_id">the user ID</param>
		/// <param name="max_item_id">the maximum item ID</param>
		/// <returns>a list sorted list of item IDs</returns>
		static public IList<int> PredictItems(IRecommender recommender, int user_id, int max_item_id)
		{
			var items = new int[max_item_id + 1];
			for (int i = 0; i < max_item_id; i++)
				items[i] = i;
			return PredictItems(recommender, user_id, items);
		}

		/// <summary>Predict items for a given user</summary>
		/// <param name="recommender">the recommender to use</param>
		/// <param name="user_id">the numerical ID of the user</param>
		/// <param name="relevant_items">a collection of numerical IDs of relevant items</param>
		/// <returns>an ordered list of items, the most likely item first</returns>
		static public IList<int> PredictItems(IRecommender recommender, int user_id, IList<int> relevant_items)
		{
			var result = new WeightedItem[relevant_items.Count];
			for (int i = 0; i < relevant_items.Count; i++)
			{
				int item_id = relevant_items[i];
				result[i] = new WeightedItem(item_id, recommender.Predict(user_id, item_id));
			}
			Array.Sort(result);
			Array.Reverse(result);

			var return_array = new int[result.Length];
			for (int i = 0; i < return_array.Length; i++)
				return_array[i] = result[i].item_id;
			
			return return_array;
		}
	}
}