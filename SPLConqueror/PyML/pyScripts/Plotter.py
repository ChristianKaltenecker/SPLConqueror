#!/bin/python3

import sys
import os
import matplotlib.pyplot as plt
import pandas as pd
import numpy as np
from typing import List, Dict, Tuple

COLORS = ['tab:gray', 'tab:green', 'tab:red', 'tab:green', 'tab:red']
STYLES = [':', '-', '-', '--', '--']


def print_usage() -> None:
    print("Usage: ./Plotter.py <pathToDataFile> <directoryForPlotOutput>")
    print("pathToDataFile\t The path to the .csv-file containing the data to plot")
    print("directoryForPlotOutput\t The directory where the plots should be written to")


def read_csv_file(path : str) -> pd.DataFrame:
    return pd.read_csv(path, sep=";")


def plot(file_path : str, dataframe : pd.DataFrame) -> None:
    filename, sufix = os.path.splitext(file_path)
    plt.figure(figsize=(11.69,8.27))
    with pd.plotting.plot_params.use('x_compat', True):
        columns = dataframe.columns[1:]
        for i in range(0, len(columns)):
            if i == round(len(columns) / 2):
                plt.savefig(filename + "_1" + sufix)
                plt.figure(figsize=(11.69, 8.27))
            ax = dataframe[columns[i]].plot(color=COLORS[i % len(COLORS)], style=STYLES[i % len(STYLES)],
                                            xticks=dataframe.index, rot=75)
            ax.set_xticklabels(dataframe.Term)
            ax.legend(loc='upper center', bbox_to_anchor=(0.5, 1.00))
        plt.savefig(filename + "_2" + sufix)


def main():
    if len(sys.argv) != 3:
        print_usage()
        exit(0)

    path : str = sys.argv[1]
    filePath : str = sys.argv[2]

    df : pd.DataFrame = read_csv_file(path)
    plot(filePath, df)


if __name__ == "__main__":
    main()
