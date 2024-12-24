#include <mpi.h>
#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include <iostream>

#define N 10

void initArray(int* arr, int n, int a, int b) {
	int i;
	for (i = 0; i < n; i++) arr[i] = rand() % (b - a + 1) + a; // генерация случайного числа [a;b];
}

void printArray(int* arr, int i_start, int i_end) {
	printf("\nArray: [ ");
	for (int i = i_start; i <= i_end; i++) printf("%d ", arr[i]);
	printf(" ]\n");
}

int main(int argc, char** argv) {
	/*
	* myid - id процессора = rank
	* numprocs - общее кол-во запусщенных процессов
	* i_start - индекс начала чтения массива для каждого процессора
	* i_end - индекс конца чтения массива для каждого процессора
	* interval - кол-во элементов для считывания для каждого процессора кроме последенго 
		(берет все оставшиеся значения)
	*/
	int myid, numprocs, i_start, i_end, interval;
	int arr[N]; // массив с числами для суммирования
	/*sum_proc
	* sum_proc - сумма для процессора для его заданного интервала
	* sum_all - собираемая сумма главным процессором
	*/
	int sum_proc, sum_all;
	double starttwtime = 0.0, endwtime;

	int namelen;
	char processor_name[MPI_MAX_PROCESSOR_NAME];
	MPI_Status status;
	MPI_Init(&argc, &argv);
	MPI_Comm_size(MPI_COMM_WORLD, &numprocs);
	MPI_Comm_rank(MPI_COMM_WORLD, &myid);
	MPI_Get_processor_name(processor_name, &namelen);

	fprintf(stderr, "# Process id:%d on %s was initialised\n",
		myid, processor_name);

	if (myid == 0) {
		srand((unsigned)time(NULL)); // меняем сердечник у генератора, берем системное время
		int a = 1;
		int b = 100;
		initArray(arr, N, a, b);
		printArray(arr, 0, N - 1);
		starttwtime = MPI_Wtime();
	}
	
	MPI_Bcast(arr, N, MPI_INT, 0, MPI_COMM_WORLD);

	interval = N / numprocs;
	i_start = myid * interval <= N - 1 ? myid * interval : -1;
	i_end = myid != numprocs - 1 ? i_start + interval - 1: N - 1;

	sum_proc = 0;
	if (i_start > -1) {
		fprintf(stderr, "+ Process id:%d on %s take the indexes[%d:%d]\n",
			myid, processor_name, i_start, i_end);
		for (int i = i_start; i <= i_end; i++) {
			sum_proc += arr[i];
		}
	}

	MPI_Reduce(&sum_proc, &sum_all, 1, MPI_INT, MPI_SUM, 0, MPI_COMM_WORLD);

	if (myid == 0) {
		printf("Sum of array = %d\n",
			sum_all);
		endwtime = MPI_Wtime();
		printf("Wall clock time = %f\n",
			endwtime - starttwtime);
	}
	MPI_Finalize();
	return 0;
}