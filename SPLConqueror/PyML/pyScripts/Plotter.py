#!/bin/python3

import sys
import os
from os.path import isfile, join
import matplotlib.pyplot as plt
from matplotlib import rcParams
import pandas as pd
import numpy as np
from typing import List, Dict, Tuple
import seaborn as sns

COLORS = ['tab:gray', 'tab:green', 'tab:red', 'tab:green', 'tab:red']
STYLES = [':', '-', '-', '--', '--']

FILE_NAME_SEPARATOR = "_"

# HEIGHT = 8.27
HEIGHT = 10


def print_usage() -> None:
    print("Usage: ./Plotter.py <pathToDataDir> <directoryForPlotOutput>")
    print("pathToDataDir\t The path to the directory containing the .csv-files to plot")
    print("directoryForPlotOutput\t The directory where the plots should be written to")


def read_csv_file(path: str) -> pd.DataFrame:
    return pd.read_csv(path, sep=";")



def plot(file_path: str, inputPaths: Dict[str, str], onlyBest: bool = True) -> None:
    # Read in the files
    info: Dict[str, pd.DataFrame] = {}
    for key in inputPaths.keys():
        info[key] = read_csv_file(inputPaths[key])

    # Replace the IDs
    mapping = {}
    strategy_list = []
    for index, row in info["mapping"].iterrows():
        mapping[row["ID"]] = row["Name"]
        strategy_list.append(row["Name"])

    info["inf"]["Strategy"].replace(mapping, inplace=True)
    info["inf"]["Result"] = info["inf"]["Result"].apply(abs)
    info["freq"]["Strategy"].replace(mapping, inplace=True)


    # Output file
    filename, sufix = os.path.splitext(file_path)

    # Generate the plot
    rcParams.update({'figure.autolayout': True})
    sns.set(font_scale=0.5, style="whitegrid")

    if onlyBest:
        plt.figure(figsize=(11.69, HEIGHT))
        df = info["inf"]

        wp_influence = df[df["Strategy"] == strategy_list[0]]
        ticks = wp_influence["Term"].unique()

        df["Result"] = df["Result"] * 100

        ax = sns.factorplot(x="Term", y="Result", hue="Strategy", legend=False, data=df, kind="box")
        ax.set_xticklabels(ticks, rotation=90)

        ax.set_ylabels("Relative Influence [in %]")

        ax.despine(left=True)
        plt.legend(loc='upper left')

        plt.savefig(filename + "_1" + sufix, bbox_inches='tight')
        plt.close()

        plt.figure(figsize=(11.69, HEIGHT))

        df = info["freq"]

        df["Result"] = df["Result"] * 100

        ax = sns.factorplot(x="Term", y="Result", hue="Strategy", legend=False, data=df, kind="bar")

        ax.set_xticklabels(ticks, rotation=90)
        ax.set_ylabels("Relative Frequency [in %]")

        ax.despine(left=True)
        plt.legend(loc='upper right')

        plt.savefig(filename + "_2" + sufix)
        plt.close()
    else:
        plt.figure(figsize=(11.69, HEIGHT))
        df = info["inf"]

        df["Result"] = df["Result"] * 100

        wp_influence = df[df["Strategy"] == strategy_list[0]]
        ticks = wp_influence["Term"]

        ax = sns.factorplot(x="Term", y="Result" , hue="Strategy", legend=False, data=df, kind="box")

        ax.set_xticklabels(ticks, rotation=90)
        ax.set_ylabels("Relative Influence [in %]")

        ax.despine(left=True)
        plt.legend(loc='upper right')

        plt.savefig(filename + "_1" + sufix)
        plt.close()

        plt.figure(figsize=(11.69, HEIGHT))
        df = info["freq"]

        df["Result"] = df["Result"] * 100

        wp_influence = df[df["Strategy"] == strategy_list[0]]
        ticks = wp_influence["Term"]

        ax = sns.factorplot(x="Term", y="Result", hue="Strategy", data=df, legend=False, kind="box")

        ax.set_xticklabels(ticks, rotation=90)
        ax.set_ylabels("Relative Frequency [in %]")

        ax.despine(left=True)
        plt.legend(loc='upper right')

        plt.savefig(filename + "_2" + sufix)
        plt.close()





def collect_files(path: str) -> Dict[str, Dict[str, Dict[str, Dict]]]:
    result: Dict = {}
    files: List[str] = [f for f in os.listdir(path) if isfile(join(path, f))]

    for file in files:
        if (file.startswith("plot")):
            continue
        file_name = file.split(".")[0]
        split = file_name.split(FILE_NAME_SEPARATOR)
        insert_to_dict(os.path.join(path, file), split, result)
    return result


def insert_to_dict(file_path: str, split: List[str], dict_to_fill: Dict):
    to_insert: str = split[0]

    if (len(split) == 1):
        dict_to_fill[to_insert] = file_path
        return

    if to_insert not in dict_to_fill:
        dict_to_fill[to_insert] = {}
    insert_to_dict(file_path, split[1:], dict_to_fill[to_insert])


def main():
    if len(sys.argv) != 3:
        print_usage()
        exit(0)

    path = sys.argv[1]
    output_file_path = sys.argv[2]

    filename, sufix = os.path.splitext(output_file_path)

    files: Dict[str, Dict[str, Dict[str, Dict]]] = collect_files(path)

    for case_study in files.keys():
        print(case_study)
        for size in files[case_study].keys():
            for kind in files[case_study][size].keys():
                # Only best
                if (kind == "best"):
                    plot(filename + "_" + case_study + "_" + size + "_best" + sufix, files[case_study][size]["best"])
                else:
                    plot(filename + "_" + case_study + "_" + size + "_all" + sufix, files[case_study][size]["all"], onlyBest=False)


if __name__ == "__main__":
    main()
