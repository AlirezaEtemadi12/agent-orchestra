import { ConditionalOperator, GridifyQueryBuilder } from 'gridify-client';

export type TableFilter = {
  field: string;
  value: string;
};

export type BuildGridifyQueryInput = {
  page: number;
  pageSize: number;
  orderBy: string;
  search?: string;
  searchableFields?: readonly string[];
  filters?: readonly TableFilter[];
};

export type TableQuery = {
  page: number;
  pageSize: number;
  orderBy: string;
  filter: string;
};

/**
 * Plan 0010 D8: the search is a group of case-insensitive `contains` conditions joined with OR;
 * the page's exact filters join the group with AND. The builder escapes every value itself.
 */
export function buildGridifyQuery(input: BuildGridifyQueryInput): TableQuery {
  const builder = new GridifyQueryBuilder();

  builder.setPage(input.page).setPageSize(input.pageSize);

  for (const part of input.orderBy.split(',').map((value) => value.trim()).filter(Boolean)) {
    const [field, direction] = part.split(/\s+/);

    builder.addOrderBy(field, direction?.toLowerCase() === 'desc');
  }

  const search = input.search?.trim();
  let hasCondition = false;

  if (search && input.searchableFields?.length) {
    builder.startGroup();
    input.searchableFields.forEach((field, index) => {
      if (index > 0) {
        builder.or();
      }

      builder.addCondition(field, ConditionalOperator.Contains, search, false);
    });
    builder.endGroup();
    hasCondition = true;
  }

  for (const filter of input.filters ?? []) {
    if (!filter.value) {
      continue;
    }

    if (hasCondition) {
      builder.and();
    }

    builder.addCondition(filter.field, ConditionalOperator.Equal, filter.value, true);
    hasCondition = true;
  }

  const query = builder.build();

  return {
    page: query.page,
    pageSize: query.pageSize,
    orderBy: query.orderBy ?? '',
    filter: query.filter ?? '',
  };
}
